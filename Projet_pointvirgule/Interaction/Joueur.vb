Option Strict On
Option Explicit On

''' <summary>
''' Le joueur pourra se deplacer dans le bureau et inspecter le code
''' </summary>

Public Class Joueur
    'Attribut de la classe joueur
    Public Property PositionX As Single
    Public Property PositionY As Single

    'Methode de la classe Joueur pour deplacer le joueur
    Public Sub SeDeplacer(direction As Direction)
        'L'etudiant 4 sera charger de gerer le deplacement, limites du bureau, collisionsavec les murs
        Throw New NotImplementedException("A faire par l'etudiant 4")

    End Sub

    'Methode de la classe joueur pour inspecter une ligne
    Public Function Inspecter(ligne As CodeLine) As Boolean
        If ligne Is Nothing Then
            Throw New ArgumentNullException("ligne")
        End If
        Return ligne.EstErreur
    End Function

End Class
