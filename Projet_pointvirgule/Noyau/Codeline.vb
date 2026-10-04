Public Class CodeLine
    'Attibut de la classe CodeLine
    Public ReadOnly NumeroLigne As Integer
    Public ReadOnly Contenu As String
    Public ReadOnly EstErreur As Boolean
    'Attirbut pour l'affichage
    Public Property PositionX As Double
    Public Property PositionY As Double

    'Constructeur et instanciation pour la Classe Codeline
    Public Sub New(numeroligne As Integer, contenu As String, estErreur As Boolean)
        Me.NumeroLigne = numeroligne
        Me.Contenu = contenu
        Me.EstErreur = estErreur
    End Sub



End Class
