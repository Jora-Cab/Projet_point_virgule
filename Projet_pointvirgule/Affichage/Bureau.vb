Option Strict On
Option Explicit On

Imports System.Drawing


''' <summary>
''' La piece a 4 murs couvert de code
''' </summary>

Public Class Bureau
    Implements IRenduBureau

    'Attribut de la classe Bureau
    Private ReadOnly _murs As New List(Of Mur)

    Public ReadOnly Property Murs As IReadOnlyList(Of Mur)
        Get
            Return _murs
        End Get
    End Property

    'Constructeur qui cree les 04 murs du bureau
    Public Sub New()
        _murs.Add(New Mur(Orientation.Nord))
        _murs.Add(New Mur(Orientation.Sud))
        _murs.Add(New Mur(Orientation.Est))
        _murs.Add(New Mur(Orientation.Ouest))
    End Sub

    'Methode de la classe Bureau pour Generer le contenu
    Public Sub GenererContenu(generateur As IGenerateurCode, nombreLignes As Integer)
        '(Etudiant 3) : appelle generateur.GenererLignesValides
        'puis repartir les lignes sur les 04 murs
        Throw New NotImplementedException("Travail de l'etudiant 3")
    End Sub

    'Methode de la classe Bureau pour placer l'erreur dans les lignes de code
    Public Sub PlacerErreur()
        '(Etudiant 2 et 3) : ils doivent choisir une ligne au hasard
        ' generateur.CasserLigne, puis Mur.RemplacerLigne
        Throw New NotImplementedException("Travail de les etudiants 2 et 3")
    End Sub

    'Methode de IRenduBureau : dessin des lignes visibles
    Public Sub Dessiner(g As Graphics, zoneVisible As Rectangle) Implements IRenduBureau.Dessiner
        'Etudiant 3 : viewport, dessiner seulement les lignes visibles
        Throw New NotImplementedException("A faire par l'etudiant 3")
    End Sub

    'Methode de IRenduBureau : quelle ligne est a cette position
    Public Function ObtenirLigneALaPosition(x As Single, y As Single) As CodeLine Implements IRenduBureau.ObtenirLigneALaPosition
        Throw New NotImplementedException("A faire par l'etudiant 3")
    End Function
End Class
