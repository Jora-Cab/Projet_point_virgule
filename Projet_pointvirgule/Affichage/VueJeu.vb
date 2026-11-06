Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

''' <summary>
''' Vue 3D a la premiere personne : feuilles de papier, loupe, bras du joueur, regard haut/bas.
''' </summary>
Public Class VueJeu
    Inherits Control

    Private Declare Function GetAsyncKeyState Lib "user32" (ByVal touche As Integer) As Short

    Private Const LargeurColonne As Integer = 3

    'Champ de vision normal et zoome
    Private Const ChampNormal As Double = 0.66
    Private Const ChampZoom As Double = 0.22

    'La loupe ne marche que si le mur vise est a moins de cette distance (en cases)
    Private Const DistanceZoomMax As Double = 4.0

    'Vitesse et amplitude du regard haut/bas
    Private Const VitesseInclinaison As Double = 0.03
    Private Const AmplitudeInclinaison As Double = 0.35

    'Nombre de feuilles cote a cote sur une face de mur
    Private Const FeuillesParFace As Integer = 6

    'Un mur touche par un rayon
    Private Structure ImpactMur
        Public Distance As Double
        Public Cote As Integer
        Public Genre As Char
        Public Position As Double
        Public CaseX As Integer
        Public CaseY As Integer
        Public Face As Integer
    End Structure

    'TEMPORAIRE : noms de fichiers affiches dans la bulle (la CodeLine n'a pas de nom de fichier)
    Private ReadOnly _noms As String() = {
        "export_biling_period", "vendor_shipment", "Invoice_Normalizer",
        "Session_Client", "Inventory_Sync", "Audit_Queue",
        "Contact_Processor", "Print_Spool"
    }

    Private _carte As Carte
    Private _joueur As Joueur
    Private _bureau As Bureau
    Private WithEvents _minuteur As New System.Windows.Forms.Timer()

    'Zoom de la loupe : 0 = aucun, 1 = maximal
    Private _zoom As Double = 0.0

    'Regard : -1 = tout en bas, 0 = a plat, 1 = tout en haut
    Private _inclinaison As Double = 0.0

    'Distance du mur situe au centre de l'ecran (calculee a chaque image)
    Private _distanceMurVise As Double = 1.0E+30
    Private _distanceEnCours As Double

    'Feuille visee par le centre de l'ecran
    Private _cibleValide As Boolean
    Private _cibleNom As String
    Private _cibleDebut As Integer

    Public Sub New()
        Me.DoubleBuffered = True
        Me.ResizeRedraw = True
        Me.BackColor = Color.Black
        _minuteur.Interval = 16
        _minuteur.Start()
    End Sub

    Public Sub Configurer(carte As Carte, joueur As Joueur, bureau As Bureau)
        _carte = carte
        _joueur = joueur
        _bureau = bureau
        _joueur.Carte = carte
        Invalidate()
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _minuteur.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    Private Function Appuyee(touche As Keys) As Boolean
        Return GetAsyncKeyState(CInt(touche)) < 0
    End Function

    'Toutes les 16 ms : clavier, deplacement, regard, zoom
    Private Sub _minuteur_Tick(sender As Object, e As EventArgs) Handles _minuteur.Tick
        If _joueur Is Nothing OrElse Form.ActiveForm IsNot Me.FindForm() Then
            Return
        End If

        Dim bouge As Boolean = False
        If Appuyee(Keys.Up) OrElse Appuyee(Keys.Z) Then
            _joueur.SeDeplacer(Direction.Haut)
            bouge = True
        End If
        If Appuyee(Keys.Down) OrElse Appuyee(Keys.S) Then
            _joueur.SeDeplacer(Direction.Bas)
            bouge = True
        End If
        If Appuyee(Keys.Left) OrElse Appuyee(Keys.Q) Then
            _joueur.SeDeplacer(Direction.Gauche)
            bouge = True
        End If
        If Appuyee(Keys.Right) OrElse Appuyee(Keys.D) Then
            _joueur.SeDeplacer(Direction.Droite)
            bouge = True
        End If

        'Regard vers le haut, vers le bas, ou remis a plat
        If Appuyee(Keys.PageUp) OrElse Appuyee(Keys.R) Then
            _inclinaison = Math.Min(1.0, _inclinaison + VitesseInclinaison)
            bouge = True
        End If
        If Appuyee(Keys.PageDown) OrElse Appuyee(Keys.F) Then
            _inclinaison = Math.Max(-1.0, _inclinaison - VitesseInclinaison)
            bouge = True
        End If
        If Appuyee(Keys.Home) Then
            _inclinaison = 0.0
            bouge = True
        End If

        'La loupe : clic droit ou E, mais seulement si le mur vise est assez proche
        Dim loupe As Boolean = (Appuyee(Keys.RButton) OrElse Appuyee(Keys.E)) AndAlso
                               _distanceMurVise <= DistanceZoomMax
        Dim objectif As Double = 0.0
        If loupe Then
            objectif = 1.0
        End If
        If Math.Abs(_zoom - objectif) > 0.005 Then
            _zoom += (objectif - _zoom) * 0.25
            bouge = True
        ElseIf _zoom <> objectif Then
            _zoom = objectif
            bouge = True
        End If

        If bouge Then
            Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        If _carte Is Nothing OrElse _joueur Is Nothing Then
            Return
        End If

        Dim g As Graphics = e.Graphics
        Dim w As Integer = Me.ClientSize.Width
        Dim h As Integer = Me.ClientSize.Height
        If w < 10 OrElse h < 10 Then
            Return
        End If

        'L'horizon descend quand on regarde en haut, et monte quand on regarde en bas
        Dim horizon As Double = h / 2.0 + _inclinaison * h * AmplitudeInclinaison

        DessinerPlafondEtSol(g, w, h, horizon)
        DessinerMurs(g, w, h, horizon)
        DessinerSurcouche(g, w, h)
    End Sub

    Private Sub DessinerPlafondEtSol(g As Graphics, w As Integer, h As Integer, horizon As Double)
        Dim moitie As Integer = CInt(Math.Max(2.0, Math.Min(h - 2.0, horizon)))

        Using ciel As New LinearGradientBrush(New Rectangle(0, 0, w, moitie + 1),
                Color.FromArgb(250, 248, 240), Color.FromArgb(190, 186, 176), LinearGradientMode.Vertical)
            g.FillRectangle(ciel, 0, 0, w, moitie)
        End Using

        Using sol As New LinearGradientBrush(New Rectangle(0, moitie, w, h - moitie + 1),
                Color.FromArgb(70, 55, 42), Color.FromArgb(140, 112, 82), LinearGradientMode.Vertical)
            g.FillRectangle(sol, 0, moitie, w, h - moitie)
        End Using
    End Sub

    Private Function HauteurDe(genre As Char) As Double
        If genre = "C"c Then
            Return 0.5
        End If
        Return 1.0
    End Function

    'Lance un rayon par colonne d'ecran et dessine les murs touches
    Private Sub DessinerMurs(g As Graphics, w As Integer, h As Integer, horizon As Double)
        _cibleValide = False
        _distanceEnCours = 1.0E+30

        Dim champ As Double = ChampNormal + (ChampZoom - ChampNormal) * _zoom
        Dim facteur As Double = ChampNormal / champ

        Dim dirX As Double = Math.Cos(_joueur.Angle)
        Dim dirY As Double = Math.Sin(_joueur.Angle)
        Dim planX As Double = -dirY * champ
        Dim planY As Double = dirX * champ
        Dim nbColonnes As Integer = w \ LargeurColonne + 1
        Dim colCentre As Integer = nbColonnes \ 2
        Dim centreEcran As Double = h / 2.0

        For col As Integer = 0 To nbColonnes - 1
            Dim cameraX As Double = 2.0 * col / nbColonnes - 1.0
            Dim rayX As Double = dirX + planX * cameraX
            Dim rayY As Double = dirY + planY * cameraX

            Dim caseX As Integer = CInt(Math.Floor(_joueur.PositionX))
            Dim caseY As Integer = CInt(Math.Floor(_joueur.PositionY))

            Dim deltaX As Double = 1.0E+30
            If rayX <> 0 Then
                deltaX = Math.Abs(1.0 / rayX)
            End If
            Dim deltaY As Double = 1.0E+30
            If rayY <> 0 Then
                deltaY = Math.Abs(1.0 / rayY)
            End If

            Dim pasX As Integer
            Dim pasY As Integer
            Dim distX As Double
            Dim distY As Double

            If rayX < 0 Then
                pasX = -1
                distX = (_joueur.PositionX - caseX) * deltaX
            Else
                pasX = 1
                distX = (caseX + 1.0 - _joueur.PositionX) * deltaX
            End If
            If rayY < 0 Then
                pasY = -1
                distY = (_joueur.PositionY - caseY) * deltaY
            Else
                pasY = 1
                distY = (caseY + 1.0 - _joueur.PositionY) * deltaY
            End If

            Dim impacts As New List(Of ImpactMur)
            Dim cote As Integer = 0
            Dim face As Integer = 0
            Dim compteur As Integer = 0
            Dim fini As Boolean = False

            While Not fini AndAlso compteur < 64
                If distX < distY Then
                    distX += deltaX
                    caseX += pasX
                    cote = 0
                    If pasX > 0 Then
                        face = 0
                    Else
                        face = 1
                    End If
                Else
                    distY += deltaY
                    caseY += pasY
                    cote = 1
                    If pasY > 0 Then
                        face = 2
                    Else
                        face = 3
                    End If
                End If
                compteur += 1

                Dim genre As Char = _carte.TypeCase(caseX, caseY)
                If genre = "#"c OrElse genre = "C"c Then
                    Dim impact As New ImpactMur()
                    If cote = 0 Then
                        impact.Distance = distX - deltaX
                    Else
                        impact.Distance = distY - deltaY
                    End If
                    impact.Distance = Math.Max(impact.Distance, 0.05)
                    impact.Cote = cote
                    impact.Genre = genre

                    Dim pos As Double
                    If cote = 0 Then
                        pos = _joueur.PositionY + impact.Distance * rayY
                    Else
                        pos = _joueur.PositionX + impact.Distance * rayX
                    End If
                    impact.Position = pos - Math.Floor(pos)
                    impact.CaseX = caseX
                    impact.CaseY = caseY
                    impact.Face = face

                    impacts.Add(impact)
                    If genre = "#"c Then
                        fini = True
                    End If
                End If
            End While

            'Dessin du plus loin au plus proche
            For i As Integer = impacts.Count - 1 To 0 Step -1
                Dim imp As ImpactMur = impacts(i)
                Dim hauteurMur As Double = h / imp.Distance * facteur
                Dim bas As Double = horizon + hauteurMur / 2.0
                Dim haut As Double = bas - hauteurMur * HauteurDe(imp.Genre)

                Dim couleurBase As Color = Color.FromArgb(215, 200, 170)
                If imp.Genre = "C"c Then
                    couleurBase = Color.FromArgb(32, 52, 92)
                End If

                Dim ombre As Double = 1.0 / (1.0 + imp.Distance * 0.12)
                If imp.Cote = 1 Then
                    ombre *= 0.8
                End If

                Dim couleur As Color = Color.FromArgb(CInt(couleurBase.R * ombre),
                                                      CInt(couleurBase.G * ombre),
                                                      CInt(couleurBase.B * ombre))
                DessinerColonne(g, col, haut, bas, imp, ombre, couleur, col = colCentre, centreEcran)
            Next
        Next

        'On retient la distance du mur vise pour decider si la loupe peut s'activer
        _distanceMurVise = _distanceEnCours
    End Sub

    'Indique ou commence la lecture pour cette feuille (position dans la liste de toutes les lignes)
    Private Function DebutFeuille(imp As ImpactMur, k As Integer, r As Integer) As Integer
        If _bureau Is Nothing Then
            Return 0
        End If
        Dim total As Integer = _bureau.NombreLignes
        If total = 0 Then
            Return 0
        End If

        'Chaque feuille possible (case, face, rangee, colonne) a un numero unique
        Dim nbNumeros As Long = CLng(_carte.Largeur) * _carte.Hauteur * 4 * 18
        Dim numero As Long = ((CLng(imp.CaseY) * _carte.Largeur + imp.CaseX) * 4 + imp.Face) * 18 +
                             r * FeuillesParFace + k

        Return CInt(numero * total \ nbNumeros)
    End Function

    'Dessine une colonne de mur, ses petites feuilles, et repere la feuille visee au centre
    Private Sub DessinerColonne(g As Graphics, col As Integer, haut As Double, bas As Double,
                                imp As ImpactMur, ombre As Double, couleurMur As Color,
                                estCentre As Boolean, centreEcran As Double)
        Dim x As Single = CSng(col * LargeurColonne)
        Dim hauteurVisible As Double = bas - haut

        Using pinceauMur As New SolidBrush(couleurMur)
            g.FillRectangle(pinceauMur, x, CSng(haut), CSng(LargeurColonne), CSng(hauteurVisible))
        End Using

        'Ce mur est celui qui se trouve au centre de l'ecran (le plus proche gagne)
        If estCentre AndAlso haut <= centreEcran AndAlso centreEcran <= bas Then
            _cibleValide = False
            _distanceEnCours = imp.Distance
        End If

        If hauteurVisible < 40 Then
            Return
        End If

        Dim k As Integer = CInt(Math.Floor(imp.Position * FeuillesParFace))
        If k > FeuillesParFace - 1 Then
            k = FeuillesParFace - 1
        End If
        Dim u As Double = imp.Position * FeuillesParFace - k

        If u < 0.1 OrElse u > 0.9 Then
            Return
        End If
        Dim dansFeuille As Double = (u - 0.1) / 0.8

        Dim nbRangees As Integer = 3
        If imp.Genre = "C"c Then
            nbRangees = 2
        End If

        Using papier As New SolidBrush(Color.FromArgb(CInt(250 * ombre), CInt(246 * ombre), CInt(232 * ombre)))
            Using encre As New SolidBrush(Color.FromArgb(CInt(70 * ombre), CInt(70 * ombre), CInt(80 * ombre)))

                For r As Integer = 0 To nbRangees - 1
                    Dim v0 As Double = r / CDbl(nbRangees) + 0.06
                    Dim v1 As Double = (r + 1) / CDbl(nbRangees) - 0.06
                    Dim y0 As Single = CSng(haut + hauteurVisible * v0)
                    Dim y1 As Single = CSng(haut + hauteurVisible * v1)
                    Dim hauteurFeuille As Single = y1 - y0

                    g.FillRectangle(papier, x, y0, CSng(LargeurColonne), hauteurFeuille)

                    If hauteurFeuille > 18 Then
                        Dim nbLignes As Integer = 5
                        Dim graine As Integer = Math.Abs((imp.CaseX * 31 + imp.CaseY * 17 + k * 7 + r * 3) Mod 11)

                        For n As Integer = 1 To nbLignes
                            Dim longueur As Double = 0.45 + 0.5 * (((graine + n * 5) Mod 7) / 6.0)
                            If dansFeuille > 0.06 AndAlso dansFeuille < longueur Then
                                Dim ly As Single = y0 + hauteurFeuille * n / (nbLignes + 1)
                                g.FillRectangle(encre, x, ly, CSng(LargeurColonne),
                                                Math.Max(1.0F, hauteurFeuille * 0.025F))
                            End If
                        Next
                    End If

                    'Cette feuille est-elle sous le viseur, et assez proche pour etre lue ?
                    If estCentre AndAlso imp.Distance < DistanceZoomMax AndAlso
                       y0 <= centreEcran AndAlso centreEcran <= y1 Then
                        Dim code As Integer = Math.Abs(imp.CaseX * 31 + imp.CaseY * 17 + imp.Face * 5 + k * 3 + r)
                        _cibleValide = True
                        _cibleNom = "lib/legacy/" & _noms(code Mod _noms.Length) & ".js"
                        _cibleDebut = DebutFeuille(imp, k, r)
                    End If
                Next

            End Using
        End Using
    End Sub

    Private Function Pt(w As Integer, h As Integer, fx As Double, fy As Double) As PointF
        Return New PointF(CSng(w * fx), CSng(h * fy))
    End Function

    'Par-dessus la 3D : bulle, loupe, bras du joueur, et feuille au premier plan
    Private Sub DessinerSurcouche(g As Graphics, w As Integer, h As Integer)
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim cx As Single = w / 2.0F
        Dim cy As Single = h / 2.0F
        Dim rayon As Single = CSng(h * (0.05 + 0.1 * _zoom))

        'La loupe est grisee quand le mur est trop loin pour zoomer
        Dim couleurCadre As Color = Color.FromArgb(40, 40, 44)
        If _distanceMurVise > DistanceZoomMax Then
            couleurCadre = Color.FromArgb(160, 160, 165)
        End If

        If _cibleValide Then
            Using police As New Font("Segoe UI", 9)
                Dim taille As SizeF = g.MeasureString(_cibleNom, police)
                Dim bx As Single = cx - taille.Width / 2.0F - 8
                Dim by As Single = cy - rayon - taille.Height - 14
                Using fond As New SolidBrush(Color.FromArgb(210, 20, 20, 24))
                    g.FillRectangle(fond, bx, by, taille.Width + 16, taille.Height + 4)
                End Using
                g.DrawString(_cibleNom, police, Brushes.WhiteSmoke, bx + 8, by + 2)
            End Using
        End If

        Using stylo As New Pen(couleurCadre, Math.Max(6.0F, CSng(h * 0.018)))
            stylo.StartCap = LineCap.Round
            stylo.EndCap = LineCap.Round
            g.DrawLine(stylo, cx + rayon * 0.72F, cy + rayon * 0.72F, CSng(w * 0.66), CSng(h * 0.7))
        End Using

        Using brosseManche As New SolidBrush(Color.FromArgb(70, 78, 104))
            g.FillPolygon(brosseManche, New PointF() {
                Pt(w, h, 0.66, 1.0), Pt(w, h, 1.02, 1.0), Pt(w, h, 1.02, 0.8), Pt(w, h, 0.8, 0.78)})
        End Using
        Using brossePoignet As New SolidBrush(Color.FromArgb(235, 235, 240))
            g.FillPolygon(brossePoignet, New PointF() {
                Pt(w, h, 0.66, 1.0), Pt(w, h, 0.72, 1.0), Pt(w, h, 0.86, 0.8), Pt(w, h, 0.8, 0.78)})
        End Using
        Using peau As New SolidBrush(Color.FromArgb(236, 196, 170))
            g.FillClosedCurve(peau, New PointF() {
                Pt(w, h, 0.74, 0.8), Pt(w, h, 0.76, 0.68), Pt(w, h, 0.84, 0.62),
                Pt(w, h, 0.92, 0.68), Pt(w, h, 0.93, 0.8), Pt(w, h, 0.84, 0.86)})
            g.FillClosedCurve(peau, New PointF() {
                Pt(w, h, 0.79, 0.72), Pt(w, h, 0.72, 0.64), Pt(w, h, 0.64, 0.6),
                Pt(w, h, 0.585, 0.592), Pt(w, h, 0.58, 0.618), Pt(w, h, 0.64, 0.64),
                Pt(w, h, 0.72, 0.7), Pt(w, h, 0.78, 0.76)})
        End Using
        Using peauSombre As New SolidBrush(Color.FromArgb(205, 160, 135))
            g.FillClosedCurve(peauSombre, New PointF() {
                Pt(w, h, 0.8, 0.7), Pt(w, h, 0.76, 0.64), Pt(w, h, 0.73, 0.6),
                Pt(w, h, 0.71, 0.62), Pt(w, h, 0.72, 0.68), Pt(w, h, 0.77, 0.74)})
        End Using

        Using verre As New SolidBrush(Color.FromArgb(CInt(20 + 40 * _zoom), 255, 255, 255))
            g.FillEllipse(verre, cx - rayon, cy - rayon, rayon * 2, rayon * 2)
        End Using
        Using cadre As New Pen(couleurCadre, Math.Max(4.0F, CSng(h * 0.012)))
            g.DrawEllipse(cadre, cx - rayon, cy - rayon, rayon * 2, rayon * 2)
        End Using
        g.FillEllipse(Brushes.DimGray, cx - 3, cy - 3, 6, 6)

        If _zoom > 0.85 AndAlso _cibleValide Then
            DessinerCarte(g, w, h)
        End If
    End Sub

    'La feuille au premier plan : les vraies lignes de code du bureau
    Private Sub DessinerCarte(g As Graphics, w As Integer, h As Integer)
        If _bureau Is Nothing Then
            Return
        End If

        Dim cw As Single = CSng(w * 0.46)
        Dim ch As Single = CSng(h * 0.8)
        Dim x0 As Single = CSng(w * 0.05)
        Dim y0 As Single = CSng(h * 0.1)

        Using ombre As New SolidBrush(Color.FromArgb(110, 0, 0, 0))
            g.FillRectangle(ombre, x0 + 8, y0 + 8, cw, ch)
        End Using
        Using papier As New SolidBrush(Color.FromArgb(250, 246, 232))
            g.FillRectangle(papier, x0, y0, cw, ch)
        End Using
        g.DrawRectangle(Pens.Gray, x0, y0, cw, ch)

        'Le texte ne doit pas depasser de la feuille
        g.SetClip(New RectangleF(x0, y0, cw, ch))

        Using police As New Font("Consolas", 8)
            g.DrawString(_cibleNom, police, Brushes.DimGray, x0 + 10, y0 + 8)

            Dim total As Integer = _bureau.NombreLignes
            Dim nbLignes As Integer = 18
            Dim pas As Single = (ch - 60) / nbLignes

            For i As Integer = 0 To nbLignes - 1
                Dim index As Integer = _cibleDebut + i
                If index >= total Then
                    Exit For
                End If

                Dim ligne As CodeLine = _bureau.ObtenirLigne(index)
                Dim yy As Single = y0 + 34 + i * pas
                g.DrawString(ligne.NumeroLigne.ToString(), police, Brushes.Gray, x0 + 10, yy)
                g.DrawString(ligne.Contenu, police, Brushes.Black, x0 + 64, yy)
            Next
        End Using

        g.ResetClip()
    End Sub

End Class