Option Explicit On
Option Strict On

Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Threading.Tasks

''' <summary>
''' VERSION C (rapide). La copie existe en mémoire (liste de références, une seule ligne est un nouvel objet).
''' Sur le disque, on écrit seulement le numéro de la ligne fautive : "programme de base + ce numéro" = la copie.
''' - code_base.pvm        : le programme C propre, JAMAIS modifié.
''' - partie_en_cours.txt  : quelques octets pendant la partie, supprimé à la fin.
''' </summary>
Public NotInheritable Class CodeJeu

    Private Const NomBase As String = "code_base.pvm"
    Private Const NomPartie As String = "partie_en_cours.txt"

    Private ReadOnly _cheminPartie As String
    Private ReadOnly _base As List(Of CodeLine)
    Private ReadOnly _baseLecture As ReadOnlyCollection(Of CodeLine)

    Private _courant As List(Of CodeLine)
    Private _courantLecture As ReadOnlyCollection(Of CodeLine)
    Private _indexErreur As Integer = -1
    Private _partieEnCours As Boolean

    ''' <summary>Charge le programme de base (ou le génère la toute première fois).</summary>
    Public Sub New(dossier As String, nombreLignes As Integer)
        If String.IsNullOrWhiteSpace(dossier) Then
            Throw New ArgumentException("Dossier invalide.", NameOf(dossier))
        End If
        Directory.CreateDirectory(dossier)

        _cheminPartie = Path.Combine(dossier, NomPartie)
        _base = SauvegardeCode.ChargerOuGenerer(Path.Combine(dossier, NomBase), nombreLignes)
        _baseLecture = _base.AsReadOnly()
        _courant = _base
        _courantLecture = _baseLecture

        ' Une trace restée sur le disque après un plantage est écrasée.
        SauvegardeCode.Supprimer(_cheminPartie)
    End Sub

    ''' <summary>Même chose que le constructeur, mais hors du thread de l'interface.</summary>
    Public Shared Function CreerAsync(dossier As String, nombreLignes As Integer) As Task(Of CodeJeu)
        Return Task.Run(Function() New CodeJeu(dossier, nombreLignes))
    End Function

#Region "Propriétés"

    ''' <summary>Le code affiché : le programme de base hors partie, la copie avec l'erreur pendant la partie.</summary>
    Public ReadOnly Property Courant As ReadOnlyCollection(Of CodeLine)
        Get
            Return _courantLecture
        End Get
    End Property

    Public ReadOnly Property NombreLignes As Integer
        Get
            Return _base.Count
        End Get
    End Property

    Public ReadOnly Property PartieEnCours As Boolean
        Get
            Return _partieEnCours
        End Get
    End Property

    ''' <summary>Index (0-based) de la ligne fautive, -1 hors partie.</summary>
    Public ReadOnly Property IndexErreur As Integer
        Get
            Return _indexErreur
        End Get
    End Property

    ''' <summary>La ligne fautive de la partie en cours, Nothing hors partie.</summary>
    Public ReadOnly Property LigneErreur As CodeLine
        Get
            If _indexErreur < 0 Then
                Return Nothing
            End If
            Return _courant(_indexErreur)
        End Get
    End Property

#End Region

#Region "Partie"

    Public Sub DemarrerPartie()
        ' Graine toujours différente, même si deux parties démarrent à quelques ms d'intervalle.
        DemarrerPartie(New Random(Guid.NewGuid().GetHashCode()))
    End Sub


    Public Sub DemarrerPartie(rnd As Random)
        If _partieEnCours Then
            Throw New InvalidOperationException("Une partie est déjà en cours : l'erreur est verrouillée.")
        End If
        If rnd Is Nothing Then
            Throw New ArgumentNullException(NameOf(rnd))
        End If

        ' 1. Copie en mémoire (références) : les CodeLine de la base ne sont pas touchés.
        Dim copie As New List(Of CodeLine)(_base)

        ' 2. Une ligne tirée au hasard est remplacée, DANS LA COPIE, par sa version sans point-virgule.
        Dim index As Integer = GenerateurCode.InsererErreur(copie, rnd)

        ' 3. Sur le disque : seulement le numéro de la ligne fautive (quelques octets).
        Try
            File.WriteAllText(_cheminPartie, copie(index).NumeroLigne.ToString(CultureInfo.InvariantCulture))
        Catch ex As IOException
            Debug.WriteLine("Trace de la partie non écrite : " & ex.Message)
        Catch ex As UnauthorizedAccessException
            Debug.WriteLine("Trace de la partie non écrite : " & ex.Message)
        End Try

        _courant = copie
        _courantLecture = copie.AsReadOnly()
        _indexErreur = index
        _partieEnCours = True
    End Sub

    ''' <summary>Fin de partie : la trace est supprimée, le programme de base redevient le code courant.</summary>
    Public Sub TerminerPartie()
        SauvegardeCode.Supprimer(_cheminPartie)
        _courant = _base
        _courantLecture = _baseLecture
        _indexErreur = -1
        _partieEnCours = False
    End Sub

    ''' <summary>À utiliser avec Await pour ne pas figer l'interface. Ne pas appeler en même temps que TerminerPartie.</summary>
    Public Function DemarrerPartieAsync() As Task
        Return Task.Run(Sub() DemarrerPartie())
    End Function

    Public Function TerminerPartieAsync() As Task
        Return Task.Run(Sub() TerminerPartie())
    End Function

#End Region

End Class