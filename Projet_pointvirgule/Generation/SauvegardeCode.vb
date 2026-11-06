Option Explicit On
Option Strict On

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Text

''' <summary>
''' Lecture / écriture des fichiers du programme C.
''' Format : 1re ligne "PVMC2|nombre|numeroErreur" (numeroErreur = 0 si le code est propre),
''' puis une ligne de code par ligne. Une seule ligne erronée est autorisée.
''' </summary>
Public NotInheritable Class SauvegardeCode

    Private Const Signature As String = "PVMC2"
    Private Const NombreMaxLignes As Integer = 5000000
    Private Const TailleTampon As Integer = 65536
    Private Shared ReadOnly Encodage As New UTF8Encoding(False)

    Private Sub New()
    End Sub

    ''' <summary>Dossier utilisateur du jeu, toujours accessible en écriture.</summary>
    Public Shared Function DossierParDefaut() As String
        Return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PointVirguleManquant")
    End Function

    ''' <summary>Écrit les lignes dans un fichier (0 ou 1 ligne erronée, repérée dans l'en-tête).</summary>
    Public Shared Sub Sauvegarder(chemin As String, lignes As List(Of CodeLine))
        If String.IsNullOrWhiteSpace(chemin) Then
            Throw New ArgumentException("Chemin invalide.", NameOf(chemin))
        End If
        If lignes Is Nothing Then
            Throw New ArgumentNullException(NameOf(lignes))
        End If

        Dim numeroErreur As Integer = 0
        For Each ligne As CodeLine In lignes
            If ligne.EstErreur Then
                If numeroErreur <> 0 Then
                    Throw New InvalidOperationException("Plusieurs lignes erronées : une seule est autorisée.")
                End If
                numeroErreur = ligne.NumeroLigne
            End If
        Next

        Dim dossier As String = Path.GetDirectoryName(chemin)
        If Not String.IsNullOrEmpty(dossier) Then
            Directory.CreateDirectory(dossier)
        End If

        ' Écriture dans un fichier temporaire puis remplacement : jamais de fichier à moitié écrit.
        Dim temporaire As String = chemin & ".tmp"
        Using writer As New StreamWriter(temporaire, False, Encodage, TailleTampon)
            writer.WriteLine(Signature & "|" & lignes.Count.ToString(CultureInfo.InvariantCulture) & "|" &
                             numeroErreur.ToString(CultureInfo.InvariantCulture))
            For Each ligne As CodeLine In lignes
                writer.WriteLine(ligne.Contenu)
            Next
        End Using

        If File.Exists(chemin) Then
            File.Delete(chemin)
        End If
        File.Move(temporaire, chemin)
    End Sub

    ''' <summary>Lit un fichier ; la ligne fautive (s'il y en a une) est marquée EstErreur = True.</summary>
    ''' <exception cref="InvalidDataException">Fichier invalide ou tronqué.</exception>
    Public Shared Function Charger(chemin As String) As List(Of CodeLine)
        Using reader As New StreamReader(chemin, Encodage, False, TailleTampon)
            Dim entete As String = reader.ReadLine()
            If entete Is Nothing Then
                Throw New InvalidDataException("Fichier vide.")
            End If

            Dim parties As String() = entete.Split("|"c)
            Dim nombre As Integer
            Dim numeroErreur As Integer
            If parties.Length <> 3 OrElse parties(0) <> Signature OrElse
               Not Integer.TryParse(parties(1), NumberStyles.None, CultureInfo.InvariantCulture, nombre) OrElse
               Not Integer.TryParse(parties(2), NumberStyles.None, CultureInfo.InvariantCulture, numeroErreur) OrElse
               nombre <= 0 OrElse nombre > NombreMaxLignes OrElse numeroErreur > nombre Then
                Throw New InvalidDataException("En-tête invalide.")
            End If

            Dim lignes As New List(Of CodeLine)(nombre)
            For i As Integer = 1 To nombre
                Dim texte As String = reader.ReadLine()
                If texte Is Nothing Then
                    Throw New InvalidDataException("Fichier tronqué à la ligne " & i.ToString() & ".")
                End If
                lignes.Add(New CodeLine(i, texte, i = numeroErreur))
            Next
            Return lignes
        End Using
    End Function

    ''' <summary>
    ''' Charge le programme de base, ou le génère (une seule fois) puis le sauvegarde.
    ''' La liste retournée est TOUJOURS propre : si le fichier est resté avec une erreur
    ''' (plantage en cours de partie, variante B), elle est corrigée et le fichier réécrit.
    ''' </summary>
    Public Shared Function ChargerOuGenerer(chemin As String, nombre As Integer) As List(Of CodeLine)
        If File.Exists(chemin) Then
            Try
                Dim lignes As List(Of CodeLine) = Charger(chemin)
                If lignes.Count = nombre Then
                    Dim index As Integer = TrouverErreur(lignes)
                    If index >= 0 Then
                        lignes(index) = RestaurerLigne(lignes(index))
                        Sauvegarder(chemin, lignes)
                    End If
                    Return lignes
                End If
            Catch ex As InvalidDataException
                Debug.WriteLine("Fichier invalide, régénération : " & ex.Message)
            Catch ex As IOException
                Debug.WriteLine("Lecture impossible, régénération : " & ex.Message)
            Catch ex As UnauthorizedAccessException
                Debug.WriteLine("Accès refusé, régénération : " & ex.Message)
            End Try
        End If

        Dim nouvelles As List(Of CodeLine) = GenerateurCode.GenererLignesValides(nombre)
        Try
            Sauvegarder(chemin, nouvelles)
        Catch ex As IOException
            Debug.WriteLine("Sauvegarde impossible : " & ex.Message)
        Catch ex As UnauthorizedAccessException
            Debug.WriteLine("Sauvegarde non autorisée : " & ex.Message)
        End Try
        Return nouvelles
    End Function

    ''' <summary>Retourne la ligne corrigée : le point-virgule est remis, EstErreur repasse à False.</summary>
    Public Shared Function RestaurerLigne(ligne As CodeLine) As CodeLine
        Return New CodeLine(ligne.NumeroLigne, ligne.Contenu & ";", False) With {
            .PositionX = ligne.PositionX,
            .PositionY = ligne.PositionY
        }
    End Function

    ''' <summary>Supprime un fichier ; sans erreur s'il n'existe pas.</summary>
    Public Shared Sub Supprimer(chemin As String)
        Try
            If File.Exists(chemin) Then
                File.Delete(chemin)
            End If
        Catch ex As IOException
            Debug.WriteLine("Suppression impossible : " & ex.Message)
        Catch ex As UnauthorizedAccessException
            Debug.WriteLine("Suppression non autorisée : " & ex.Message)
        End Try
    End Sub

    Private Shared Function TrouverErreur(lignes As List(Of CodeLine)) As Integer
        For i As Integer = 0 To lignes.Count - 1
            If lignes(i).EstErreur Then
                Return i
            End If
        Next
        Return -1
    End Function

End Class