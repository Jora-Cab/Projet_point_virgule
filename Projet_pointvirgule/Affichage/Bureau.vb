Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms.VisualStyles

''' <summary>
''' Le bureau : 4 murs couverts de lignes de code.
''' </summary>
Public Class Bureau
    'Inherits Mur
    Implements IRenduBureau

    Private ReadOnly _murs As New List(Of Mur)
    Private ReadOnly _alea As New Random()

    Public ReadOnly Property Murs As IReadOnlyList(Of Mur)
        Get
            Return _murs
        End Get
    End Property

    Public Sub New()
        CreerMurs()
    End Sub

    'Cree 4 murs vides (efface les anciens s'il y en avait)
    Private Sub CreerMurs()
        _murs.Clear()
        _murs.Add(New Mur(Orientation.Nord))
        _murs.Add(New Mur(Orientation.Est))
        _murs.Add(New Mur(Orientation.Sud))
        _murs.Add(New Mur(Orientation.Ouest))
    End Sub

    'Genere les lignes et les repartit en blocs consecutifs sur les 4 murs
    Public Sub GenererContenu(generateur As IGenerateurCode, nombreLignes As Integer)
        If generateur Is Nothing Then
            Throw New ArgumentNullException("generateur")
        End If
        If nombreLignes < 1 Then
            Throw New ArgumentOutOfRangeException("nombreLignes")
        End If

        CreerMurs()

        Dim lignes As List(Of CodeLine) = generateur.GenererLignesValides(nombreLignes)
        Dim lignesParMur As Integer = CInt(Math.Ceiling(lignes.Count / _murs.Count))

        For i As Integer = 0 To lignes.Count - 1
            Dim indexMur As Integer = i \ lignesParMur
            _murs(indexMur).AjouterLigne(lignes(i))
        Next
    End Sub

    'Choisit une ligne au hasard (tous murs confondus) et la remplace par sa version cassee
    Public Sub PlacerErreur(generateur As IGenerateurCode)
        If generateur Is Nothing Then
            Throw New ArgumentNullException("generateur")
        End If

        Dim total As Integer = 0
        For Each unMur As Mur In _murs
            total += unMur.Lignes.Count
        Next
        If total = 0 Then
            Throw New InvalidOperationException("Appeler GenererContenu avant PlacerErreur")
        End If

        Dim choix As Integer = _alea.Next(total)
        For Each unMur As Mur In _murs
            If choix < unMur.Lignes.Count Then
                unMur.RemplacerLigne(choix, generateur.CasserLigne(unMur.Lignes(choix)))
                Return
            End If
            choix -= unMur.Lignes.Count
        Next
    End Sub

    'Le dessin est fait par VuePiece : pas utilise ici
    Public Sub Dessiner(g As Graphics, zoneVisible As Rectangle) _
        Implements IRenduBureau.Dessiner
        Throw New NotImplementedException("Le dessin est fait par VuePiece")
    End Sub

    Public Function ObtenirLigneALaPosition(x As Single, y As Single) As CodeLine _
        Implements IRenduBureau.ObtenirLigneALaPosition
        Throw New NotImplementedException("A faire avec la page de lecture")
    End Function

    'Nombre total de lignes de code sur les 4 murs
    Public ReadOnly Property NombreLignes As Integer
        Get
            Dim total As Integer = 0
            For Each unMur As Mur In _murs
                total += unMur.Lignes.Count
            Next
            Return total
        End Get
    End Property

    'Renvoie la ligne numero "index" (de 0 a NombreLignes - 1), tous murs confondus
    Public Function ObtenirLigne(index As Integer) As CodeLine
        If index < 0 Then
            Throw New ArgumentOutOfRangeException("index")
        End If

        For Each unMur As Mur In _murs
            If index < unMur.Lignes.Count Then
                Return unMur.Lignes(index)
            End If
            index -= unMur.Lignes.Count
        Next

        Throw New ArgumentOutOfRangeException("index")
    End Function

    'Repartit des lignes deja pretes (Venant de codeJeu) sur les 4murs, en bloc consecutifs
    Public Sub RepartirLignes(lignes As IReadOnlyList(Of CodeLine))

        If lignes Is Nothing Then
            Throw New ArgumentNullException("lignes")
        End If
        If lignes.Count = 0 Then
            Throw New ArgumentNullException("Aucune ligne a repartir", "lignes")
        End If

        CreerMurs()

        Dim lignesParMur As Integer = CInt(Math.Ceiling(lignes.Count / _murs.Count))
        For i As Integer = 0 To lignes.Count - 1
            _murs(i \ lignesParMur).AjouterLigne(lignes(i))
        Next

    End Sub

End Class