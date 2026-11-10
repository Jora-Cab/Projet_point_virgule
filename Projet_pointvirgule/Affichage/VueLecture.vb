Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Page de lecture en plein ecran : le code defile, la ligne sous la souris est surlignee,
''' le clic inspecte la ligne. Tout est proportionnel a la taille de la fenetre (responsive).
''' Les lignes sont dessinees par ZoneCode.
''' </summary>
Public Class VueLecture
    Inherits Control

    ''' <summary>Declenche quand le joueur clique sur une ligne.</summary>
    Public Event LigneChoisie(ligne As CodeLine)

    Private _zone As ZoneCode
    Private _nomFichier As String = ""
    Private _glisse As Boolean

    Public Sub New()
        Me.DoubleBuffered = True
        Me.ResizeRedraw = True
        Me.BackColor = Color.FromArgb(35, 35, 40)
    End Sub

    Public Sub Configurer(zone As ZoneCode)
        _zone = zone
    End Sub

    'Ouvre la page (la zone de code garde sa position : on reprend la ou on etait)
    Public Sub Ouvrir(nomFichier As String)
        _nomFichier = nomFichier
        _glisse = False
        If _zone IsNot Nothing Then
            _zone.QuitterSurvol()
        End If
        Invalidate()
    End Sub

    Public Sub Defiler(nbLignes As Integer)
        If _zone IsNot Nothing Then
            _zone.Defiler(nbLignes)
        End If
        Invalidate()
    End Sub

#Region "Geometrie (proportionnelle a la fenetre)"

    Private Function ZonePage() As Rectangle
        Dim marge As Integer = CInt(Me.ClientSize.Width * 0.06)
        Return New Rectangle(marge, 16,
                             Math.Max(120, Me.ClientSize.Width - 2 * marge),
                             Math.Max(120, Me.ClientSize.Height - 32))
    End Function

    Private Function HauteurEntete() As Integer
        Return Math.Max(46, CInt(Me.ClientSize.Height * 0.09))
    End Function

    Private Function HauteurPied() As Integer
        Return Math.Max(30, CInt(Me.ClientSize.Height * 0.06))
    End Function

    Private Function LargeurBarre() As Integer
        Return Math.Max(12, CInt(Me.ClientSize.Width * 0.014))
    End Function

    Private Function ZoneLignes() As Rectangle
        Dim p As Rectangle = ZonePage()
        Return New Rectangle(p.X + 8, p.Y + HauteurEntete(),
                             p.Width - 16 - LargeurBarre(),
                             p.Height - HauteurEntete() - HauteurPied())
    End Function

    Private Function Piste() As Rectangle
        Dim z As Rectangle = ZoneLignes()
        Return New Rectangle(z.Right + 4, z.Top, LargeurBarre(), z.Height)
    End Function

    Private Function HauteurPoignee() As Integer
        If _zone Is Nothing OrElse _zone.Total <= 0 Then
            Return Piste().Height
        End If
        Dim h As Double = Piste().Height * _zone.NbVisibles / CDbl(_zone.Total)
        Return CInt(Math.Max(24.0, Math.Min(Piste().Height, h)))
    End Function

    Private Function Poignee() As Rectangle
        Dim p As Rectangle = Piste()
        Dim hp As Integer = HauteurPoignee()
        Dim y As Integer = p.Top
        If _zone IsNot Nothing AndAlso _zone.PremiereMax > 0 Then
            y = p.Top + CInt((p.Height - hp) * CDbl(_zone.Premiere) / _zone.PremiereMax)
        End If
        Return New Rectangle(p.X, y, p.Width, hp)
    End Function

#End Region

#Region "Dessin"

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        If _zone Is Nothing Then
            Return
        End If

        Dim g As Graphics = e.Graphics
        Dim page As Rectangle = ZonePage()
        Dim entete As Integer = HauteurEntete()
        Dim pied As Integer = HauteurPied()

        'Ombre et papier
        Using ombre As New SolidBrush(Color.FromArgb(120, 0, 0, 0))
            g.FillRectangle(ombre, page.X + 6, page.Y + 6, page.Width, page.Height)
        End Using
        Using papier As New SolidBrush(Color.FromArgb(250, 246, 232))
            g.FillRectangle(papier, page)
        End Using
        g.DrawRectangle(Pens.Gray, page)

        'Entete : nom du fichier et consignes
        Using police As New Font("Segoe UI", Math.Max(14.0F, entete * 0.36F), FontStyle.Bold, GraphicsUnit.Pixel)
            g.DrawString(_nomFichier, police, Brushes.DimGray, page.X + 14, page.Y + 6)
        End Using
        Using petite As New Font("Segoe UI", Math.Max(11.0F, entete * 0.24F), FontStyle.Regular, GraphicsUnit.Pixel)
            g.DrawString("Survolez une ligne pour la surligner, cliquez pour l'inspecter.   Molette ou flèches : défiler.   Échap : retour.",
                         petite, Brushes.Gray, page.X + 14, page.Y + entete * 0.62F)
        End Using

        'Les lignes de code
        _zone.Dessiner(g, ZoneLignes())

        'Barre de defilement
        Using fondPiste As New SolidBrush(Color.FromArgb(232, 228, 214))
            g.FillRectangle(fondPiste, Piste())
        End Using
        Using brossePoignee As New SolidBrush(Color.FromArgb(170, 165, 150))
            g.FillRectangle(brossePoignee, Poignee())
        End Using

        'Pied de page : resultat de l'inspection, ou position dans le code
        Dim texte As String = _zone.Message
        Dim pinceau As Brush = Brushes.Firebrick
        If texte = "" Then
            texte = "Ligne " & (_zone.Premiere + 1).ToString() & " sur " & _zone.Total.ToString()
            pinceau = Brushes.Gray
        ElseIf _zone.MessageReussite Then
            pinceau = Brushes.ForestGreen
        End If
        Using police As New Font("Segoe UI", Math.Max(12.0F, pied * 0.45F), FontStyle.Bold, GraphicsUnit.Pixel)
            g.DrawString(texte, police, pinceau, page.X + 14, page.Bottom - pied + (pied - police.Height) / 2.0F)
        End Using
    End Sub

#End Region

#Region "Souris"

    'Place la barre de defilement sous la souris
    Private Sub DefilerVers(y As Integer)
        If _zone Is Nothing Then
            Return
        End If
        Dim p As Rectangle = Piste()
        Dim hp As Integer = HauteurPoignee()
        Dim place As Integer = p.Height - hp
        If place <= 0 Then
            Return
        End If

        Dim fraction As Double = (y - p.Top - hp / 2.0) / place
        fraction = Math.Max(0.0, Math.Min(1.0, fraction))
        _zone.Premiere = CInt(fraction * _zone.PremiereMax)
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        MyBase.OnMouseEnter(e)
        Me.Focus()
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        If _zone Is Nothing Then
            Return
        End If

        If _glisse Then
            DefilerVers(e.Y)
        ElseIf _zone.Survoler(e.X, e.Y) Then
            Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        If _zone IsNot Nothing Then
            _zone.QuitterSurvol()
            Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        Me.Focus()
        If _zone Is Nothing OrElse e.Button <> MouseButtons.Left Then
            Return
        End If

        If Piste().Contains(e.Location) Then
            _glisse = True
            DefilerVers(e.Y)
        Else
            Dim ligne As CodeLine = _zone.ObtenirLigneALaPosition(e.X, e.Y)
            If ligne IsNot Nothing Then
                RaiseEvent LigneChoisie(ligne)
                Invalidate()
            End If
        End If
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)
        _glisse = False
    End Sub

    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
        MyBase.OnMouseWheel(e)
        If _zone Is Nothing Then
            Return
        End If
        _zone.Defiler(-Math.Sign(e.Delta) * 3)
        _zone.Survoler(e.X, e.Y)
        Invalidate()
    End Sub

#End Region

End Class
