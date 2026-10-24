Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Vue en perspective d'une piece : plafond, sol, murs de cote et mur du fond.
''' Le joueur avance, recule et se tourne vers les 4 murs.
''' </summary>
Public Class VuePiece
    Inherits Control

    'Limites de la distance au mur du fond, et taille d'un pas
    Private Const DistanceMin As Single = 0.5F
    Private Const DistanceMax As Single = 20.0F
    Private Const Pas As Single = 1.0F

    'Distance jusqu'au mur du fond (petit = proche, grand = loin)
    Private _distance As Single = 8.0F

    'Direction du regard : 0 = Nord, 1 = Est, 2 = Sud, 3 = Ouest
    Private _cap As Integer = 0

    Public Sub New()
        Me.DoubleBuffered = True
        Me.ResizeRedraw = True
        Me.BackColor = Color.Black
    End Sub

    Public Property Distance As Single
        Get
            Return _distance
        End Get
        Set(value As Single)
            _distance = Math.Max(0.0F, value)
            Invalidate()
        End Set
    End Property

    'Le mur que le joueur regarde
    Public ReadOnly Property MurRegarde As Orientation
        Get
            Select Case _cap
                Case 0
                    Return Orientation.Nord
                Case 1
                    Return Orientation.Est
                Case 2
                    Return Orientation.Sud
                Case Else
                    Return Orientation.Ouest
            End Select
        End Get
    End Property

    'Se rapprocher du mur du fond
    Public Sub Avancer()
        Distance = Math.Max(DistanceMin, _distance - Pas)
    End Sub

    'S'eloigner du mur du fond
    Public Sub Reculer()
        Distance = Math.Min(DistanceMax, _distance + Pas)
    End Sub

    'Tourner d'un quart de tour vers la droite
    Public Sub TournerDroite()
        _cap = (_cap + 1) Mod 4
        Invalidate()
    End Sub

    'Tourner d'un quart de tour vers la gauche
    Public Sub TournerGauche()
        _cap = (_cap + 3) Mod 4
        Invalidate()
    End Sub

    'Remplit un polygone avec une couleur
    Private Sub Remplir(g As Graphics, couleur As Color, points As PointF())
        Using pinceau As New SolidBrush(couleur)
            g.FillPolygon(pinceau, points)
        End Using
    End Sub

    'Dessine une grille de feuilles de papier sur un rectangle (le mur du fond)
    Private Sub DessinerFeuilles(g As Graphics, x As Single, y As Single,
                                 largeur As Single, hauteur As Single)
        Const Colonnes As Integer = 10
        Const Rangees As Integer = 5

        Dim largeurCase As Single = largeur / Colonnes
        Dim hauteurCase As Single = hauteur / Rangees
        Dim marge As Single = 0.12F

        Using papier As New SolidBrush(Color.FromArgb(250, 246, 232))
            For r As Integer = 0 To Rangees - 1
                For c As Integer = 0 To Colonnes - 1
                    Dim fx As Single = x + c * largeurCase + largeurCase * marge
                    Dim fy As Single = y + r * hauteurCase + hauteurCase * marge
                    Dim fl As Single = largeurCase * (1.0F - 2.0F * marge)
                    Dim fh As Single = hauteurCase * (1.0F - 2.0F * marge)

                    g.FillRectangle(papier, fx, fy, fl, fh)
                    g.DrawRectangle(Pens.Gray, fx, fy, fl, fh)

                    If fl > 14 Then
                        For n As Integer = 1 To 4
                            Dim ly As Single = fy + fh * n / 5
                            Dim finLigne As Single = fx + fl * 0.88F
                            If (c + r + n) Mod 3 = 0 Then
                                finLigne = fx + fl * 0.6F
                            End If
                            g.DrawLine(Pens.DarkGray, fx + fl * 0.12F, ly, finLigne, ly)
                        Next
                    End If
                Next
            Next
        End Using
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        Dim g As Graphics = e.Graphics
        Dim largeur As Single = Me.ClientSize.Width
        Dim hauteur As Single = Me.ClientSize.Height

        'k = taille du mur du fond par rapport a l'ecran : 1 = colle a l'ecran, petit = loin
        Dim k As Single = 1.0F / (1.0F + _distance / 4.0F)

        'Les 4 coins du mur du fond
        Dim x0 As Single = largeur * (1.0F - k) / 2.0F
        Dim x1 As Single = largeur - x0
        Dim y0 As Single = hauteur * (1.0F - k) / 2.0F
        Dim y1 As Single = hauteur - y0

        'Plafond
        Remplir(g, Color.FromArgb(235, 235, 230),
                {New PointF(0, 0), New PointF(largeur, 0), New PointF(x1, y0), New PointF(x0, y0)})

        'Sol (moquette)
        Remplir(g, Color.FromArgb(120, 95, 70),
                {New PointF(0, hauteur), New PointF(largeur, hauteur), New PointF(x1, y1), New PointF(x0, y1)})

        'Mur de gauche
        Remplir(g, Color.FromArgb(215, 200, 170),
                {New PointF(0, 0), New PointF(x0, y0), New PointF(x0, y1), New PointF(0, hauteur)})

        'Mur de droite
        Remplir(g, Color.FromArgb(215, 200, 170),
                {New PointF(largeur, 0), New PointF(x1, y0), New PointF(x1, y1), New PointF(largeur, hauteur)})

        'Mur du fond, puis ses feuilles, puis son contour
        Using pinceau As New SolidBrush(Color.FromArgb(200, 185, 155))
            g.FillRectangle(pinceau, x0, y0, x1 - x0, y1 - y0)
        End Using
        DessinerFeuilles(g, x0, y0, x1 - x0, y1 - y0)
        g.DrawRectangle(Pens.DimGray, x0, y0, x1 - x0, y1 - y0)

        'Texte d'information en haut a gauche
        Using police As New Font("Segoe UI", 12, FontStyle.Bold)
            g.DrawString("Vous regardez : " & MurRegarde.ToString() &
                         "   (fleches : avancer, reculer, tourner)", police, Brushes.Black, 10, 10)
        End Using
    End Sub

End Class