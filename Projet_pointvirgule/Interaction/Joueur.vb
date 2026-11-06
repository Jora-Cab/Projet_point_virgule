Option Strict On
Option Explicit On

''' <summary>
''' Le joueur : position, direction du regard, deplacement avec collisions.
''' </summary>
Public Class Joueur

    Private Const VitesseMarche As Single = 0.045F
    Private Const VitesseRotation As Single = 0.035F
    Private Const Marge As Single = 0.25F

    Public Property PositionX As Single
    Public Property PositionY As Single
    Public Property Angle As Single
    Public Property Carte As Carte

    Public Sub PlacerEn(x As Single, y As Single, angle As Single)
        PositionX = x
        PositionY = y
        Me.Angle = angle
    End Sub

    Public Sub SeDeplacer(direction As Direction)
        Select Case direction
            Case Direction.Haut
                Marcher(1.0F)
            Case Direction.Bas
                Marcher(-1.0F)
            Case Direction.Gauche
                Angle -= VitesseRotation
            Case Direction.Droite
                Angle += VitesseRotation
        End Select
    End Sub

    'Avance (sens = 1) ou recule (sens = -1), sans traverser les murs
    Private Sub Marcher(sens As Single)
        Dim dx As Single = CSng(Math.Cos(Angle)) * VitesseMarche * sens
        Dim dy As Single = CSng(Math.Sin(Angle)) * VitesseMarche * sens

        If Not EstBloque(PositionX + dx, PositionY) Then
            PositionX += dx
        End If
        If Not EstBloque(PositionX, PositionY + dy) Then
            PositionY += dy
        End If
    End Sub

    'Le joueur occupe un petit carre : on teste ses 4 coins
    Private Function EstBloque(x As Single, y As Single) As Boolean
        If Carte Is Nothing Then
            Return False
        End If
        Dim gauche As Integer = CInt(Math.Floor(x - Marge))
        Dim droite As Integer = CInt(Math.Floor(x + Marge))
        Dim haut As Integer = CInt(Math.Floor(y - Marge))
        Dim bas As Integer = CInt(Math.Floor(y + Marge))
        Return Carte.EstPlein(gauche, haut) OrElse Carte.EstPlein(droite, haut) OrElse
               Carte.EstPlein(gauche, bas) OrElse Carte.EstPlein(droite, bas)
    End Function

    'Renvoie True si la ligne inspectee est la ligne fautive
    Public Function Inspecter(ligne As CodeLine) As Boolean
        If ligne Is Nothing Then
            Throw New ArgumentNullException("ligne")
        End If
        Return ligne.EstErreur
    End Function

End Class