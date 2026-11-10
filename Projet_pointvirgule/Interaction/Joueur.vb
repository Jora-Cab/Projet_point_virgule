Option Strict On
Option Explicit On

''' <summary>
''' Le joueur : position, direction du regard, deplacement (clavier et souris) avec collisions,
''' et inspection d'une ligne de code. (Etudiant 4)
''' </summary>
Public Class Joueur

    Private Const VitesseMarche As Single = 0.045F
    Private Const VitesseRotation As Single = 0.035F
    Private Const Marge As Single = 0.25F

    Private _derniereLigne As CodeLine

    Public Property PositionX As Single
    Public Property PositionY As Single

    'Direction du regard en radians : 0 = vers l'est, Pi/2 = vers le sud
    Public Property Angle As Single

    'Le plan sert aux limites du batiment et aux collisions
    Public Property Carte As Carte

    Public ReadOnly Property DerniereLigneInspectee As CodeLine
        Get
            Return _derniereLigne
        End Get
    End Property

    Public Sub PlacerEn(x As Single, y As Single, angle As Single)
        PositionX = x
        PositionY = y
        Me.Angle = angle
    End Sub

#Region "Deplacement"

    'Deplacement au clavier : avancer, reculer, tourner
    Public Sub SeDeplacer(direction As Direction)
        Select Case direction
            Case Direction.Haut
                Marcher(1.0F)
            Case Direction.Bas
                Marcher(-1.0F)
            Case Direction.Gauche
                Tourner(-VitesseRotation)
            Case Direction.Droite
                Tourner(VitesseRotation)
        End Select
    End Sub

    'Rotation (clavier ou souris) : delta en radians, positif = vers la droite
    Public Sub Tourner(delta As Single)
        Angle += delta

        'On garde l'angle entre -Pi et Pi
        Dim tour As Single = CSng(2.0 * Math.PI)
        Do While Angle > CSng(Math.PI)
            Angle -= tour
        Loop
        Do While Angle < -CSng(Math.PI)
            Angle += tour
        Loop
    End Sub

    'Avance (sens = 1) ou recule (sens = -1), sans traverser les murs ni sortir du batiment
    Private Sub Marcher(sens As Single)
        Dim dx As Single = CSng(Math.Cos(Angle)) * VitesseMarche * sens
        Dim dy As Single = CSng(Math.Sin(Angle)) * VitesseMarche * sens

        'Deux tests separes : le joueur glisse le long d'un mur au lieu de rester colle
        If Not EstBloque(PositionX + dx, PositionY) Then
            PositionX += dx
        End If
        If Not EstBloque(PositionX, PositionY + dy) Then
            PositionY += dy
        End If
    End Sub

    'Le joueur occupe un petit carre : on teste ses 4 coins (hors du plan = mur)
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

#End Region

#Region "Inspection"

    ''' <summary>Renvoie True si la ligne inspectee est la ligne fautive.</summary>
    Public Function Inspecter(ligne As CodeLine) As Boolean
        If ligne Is Nothing Then
            Throw New ArgumentNullException("ligne")
        End If
        _derniereLigne = ligne
        Return ligne.EstErreur
    End Function

    ''' <summary>Detecte quelle ligne se trouve a la position (x, y) de l'ecran, ou Nothing s'il n'y en a pas.</summary>
    Public Function Viser(rendu As IRenduBureau, x As Single, y As Single) As CodeLine
        If rendu Is Nothing Then
            Throw New ArgumentNullException("rendu")
        End If
        Return rendu.ObtenirLigneALaPosition(x, y)
    End Function

    ''' <summary>Vise la ligne a la position (x, y) puis l'inspecte. False si aucune ligne n'est visee.</summary>
    Public Function InspecterVisee(rendu As IRenduBureau, x As Single, y As Single) As Boolean
        Dim ligne As CodeLine = Viser(rendu, x, y)
        If ligne Is Nothing Then
            Return False
        End If
        Return Inspecter(ligne)
    End Function

#End Region

End Class
