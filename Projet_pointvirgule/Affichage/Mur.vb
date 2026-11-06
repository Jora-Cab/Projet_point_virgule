Option Strict On
Option Explicit On

''' <summary>
''' Un mur du bureau, couvert de lignes de code.
''' </summary>
Public Class Mur

    'Attributs de la classe Mur
    Private ReadOnly _lignes As New List(Of CodeLine)
    Public ReadOnly Property Orientation As Orientation

    'Les autres classes peuvent lire les lignes, mais pas modifier la liste
    Public ReadOnly Property Lignes As IReadOnlyList(Of CodeLine)
        Get
            Return _lignes
        End Get
    End Property

    'Constructeur : on donne l'orientation du mur
    Public Sub New(orientation As Orientation)
        Me.Orientation = orientation
    End Sub

    'Ajoute une ligne au mur
    Public Sub AjouterLigne(ligne As CodeLine)
        If ligne Is Nothing Then
            Throw New ArgumentNullException("ligne")
        End If
        _lignes.Add(ligne)
    End Sub

    'Remplace une ligne (sert a placer la ligne cassee)
    Public Sub RemplacerLigne(index As Integer, ligne As CodeLine)
        If ligne Is Nothing Then
            Throw New ArgumentNullException("ligne")
        End If
        _lignes(index) = ligne
    End Sub

    'Renvoie seulement "nombre" lignes a partir de premiereLigne
    Public Function ObtenirLigneVisible(premiereLigne As Integer, nombre As Integer) As List(Of CodeLine)
        Dim resultat As New List(Of CodeLine)
        Dim debut As Integer = Math.Max(0, premiereLigne)
        Dim fin As Integer = Math.Min(_lignes.Count, debut + nombre)

        For i As Integer = debut To fin - 1
            resultat.Add(_lignes(i))
        Next
        Return resultat
    End Function

End Class