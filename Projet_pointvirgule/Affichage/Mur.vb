Option Strict On
Option Explicit On

''' <summary>
''' Un mur du bureau, couvert de lignes de code
''' </summary>

Public Class Mur
    'Attribut de la classe Mur
    Private ReadOnly _lignes As New List(Of CodeLine)
    Public ReadOnly Property Orientation As Orientation

    'Les autres classes pourront lire les lignes, mais pas modifier la liste
    Public ReadOnly Property Lignes As IReadOnlyList(Of CodeLine)
        Get
            Return _lignes
        End Get
    End Property

    'Constructeur de la classe Mur
    Public Sub New(orientation As Orientation)
        Dim _orientation() As String = {"Nord", "Sud", "Est", "Ouest"}
        Me.Orientation = orientation
    End Sub

    'Methode de la classe Mur pour l'ajout des lignes
    Public Sub AjouterLigne(ligne As CodeLine)

        If ligne Is Nothing Then
            Throw New ArgumentNullException("ligne")
        End If
        _lignes.Add(ligne)
    End Sub

    'Methode pour remplacer une ligne 
    Public Sub RemplacerLigne(index As Integer, ligne As CodeLine)
        If ligne Is Nothing Then
            Throw New ArgumentNullException("ligne")
        End If
        _lignes(index) = ligne
    End Sub

    'Methode de la classe Mur pour obtenir les lignes visibles
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
