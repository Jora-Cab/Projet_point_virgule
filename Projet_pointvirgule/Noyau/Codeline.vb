Public Class CodeLine
    'Attibut de la classe CodeLine
    Public ReadOnly Property NumeroLigne As Integer
    Public ReadOnly Property Contenu As String
    Public ReadOnly Property EstErreur As Boolean
    'Private i As Integer
    'Private v As String
    'Attirbut pour l'affichage
    Public Property PositionX As Single
    Public Property PositionY As Single

    'Constructeur et instanciation pour la Classe Codeline
    Public Sub New(numeroligne As Integer, contenu As String, Optional estErreur As Boolean = False)
        Me.NumeroLigne = numeroligne
        Me.Contenu = contenu
        Me.EstErreur = estErreur
    End Sub

    'Public Sub New(i As Integer, v As String)
    '    Me.i = i
    '    Me.v = v
    'End Sub
End Class
