Public Class Mur
    'Attribut de la classe Mur
    Private Property Lignes As List(Of Codeline)
    Private Property Orientation() As String

    'Constructeur de la classe Mur
    Public Sub New()
        Dim _orientation() As String = {"Nord", "Sud", "Est", "Ouest"}
        Orientation() = _orientation(3)
    End Sub

    'Methode de la classe Mur pour l'ajout des lignes
    Public Sub AjouterLigne()


    End Sub

    'Methode de la classe Mur pour obtenir les lignes visibles
    Public Sub ObtenirLigneVisible()

    End Sub

End Class
