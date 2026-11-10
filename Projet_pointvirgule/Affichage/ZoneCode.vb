Option Strict On
Option Explicit On

Imports System.Drawing

''' <summary>
''' Zone qui affiche des lignes de code avec defilement. Elle est utilisee par la feuille en gros plan
''' (zoom de la loupe) et par la page de lecture : meme affichage, meme etat, meme retour de couleur.
'''  - Responsive : la taille de la police et des lignes suit la taille de la zone.
'''  - Survol : la ligne sous la souris est surlignee en jaune.
'''  - Clic : la ligne devient verte si elle est bonne, rouge pale si elle est fausse.
'''  - Rendu a la demande : seules les lignes visibles sont dessinees.
''' Elle respecte le contrat IRenduBureau (dessiner, et detecter la ligne a une position).
''' </summary>
Public Class ZoneCode
    Implements IRenduBureau

    'Nombre de lignes que l'on souhaite voir dans la zone (la taille des lignes s'adapte)
    Private Const LignesCibles As Integer = 22

    Private ReadOnly _bureau As Bureau
    Private ReadOnly _fausses As New HashSet(Of Integer)
    Private _premiere As Integer
    Private _survolee As Integer = -1
    Private _correcte As Integer = -1
    Private _debutOuvert As Integer = -1
    Private _derniereZone As New Rectangle(0, 0, 400, 400)

    ''' <summary>Message affiche sous le code (resultat de la derniere inspection).</summary>
    Public Property Message As String = ""

    ''' <summary>True si le message est une reussite (affiche en vert).</summary>
    Public Property MessageReussite As Boolean

    Public Sub New(bureau As Bureau)
        If bureau Is Nothing Then
            Throw New ArgumentNullException("bureau")
        End If
        _bureau = bureau
    End Sub

#Region "Etat et defilement"

    Public ReadOnly Property Total As Integer
        Get
            Return _bureau.NombreLignes
        End Get
    End Property

    'Nombre de lignes visibles dans la derniere zone dessinee
    Public ReadOnly Property NbVisibles As Integer
        Get
            Return Math.Max(1, _derniereZone.Height \ HauteurLigne(_derniereZone))
        End Get
    End Property

    Public ReadOnly Property PremiereMax As Integer
        Get
            Return Math.Max(0, Total - NbVisibles)
        End Get
    End Property

    ''' <summary>Index (a partir de 0) de la premiere ligne affichee.</summary>
    Public Property Premiere As Integer
        Get
            Return _premiere
        End Get
        Set(value As Integer)
            _premiere = Math.Max(0, Math.Min(PremiereMax, value))
        End Set
    End Property

    ''' <summary>Fait defiler de nbLignes (negatif = vers le haut).</summary>
    Public Sub Defiler(nbLignes As Integer)
        Premiere += nbLignes
    End Sub

    ''' <summary>Se place au debut d'une feuille. Sans effet si on est deja sur cette feuille.</summary>
    Public Sub Ouvrir(debut As Integer)
        If debut = _debutOuvert Then
            Return
        End If
        _debutOuvert = debut
        _survolee = -1
        Message = ""
        MessageReussite = False
        Premiere = debut
    End Sub

    ''' <summary>Nouvelle partie : on efface les couleurs et le message.</summary>
    Public Sub Reinitialiser()
        _fausses.Clear()
        _correcte = -1
        _survolee = -1
        _debutOuvert = -1
        _premiere = 0
        Message = ""
        MessageReussite = False
    End Sub

    ''' <summary>Colore une ligne apres inspection : verte si elle est correcte, rouge pale sinon.</summary>
    Public Sub Marquer(index As Integer, estCorrecte As Boolean)
        If estCorrecte Then
            _correcte = index
        Else
            _fausses.Add(index)
        End If
    End Sub

    ''' <summary>Met a jour la ligne survolee. Renvoie True si elle a change (il faut redessiner).</summary>
    Public Function Survoler(x As Integer, y As Integer) As Boolean
        Dim index As Integer = IndexA(x, y)
        If index = _survolee Then
            Return False
        End If
        _survolee = index
        Return True
    End Function

    Public Sub QuitterSurvol()
        _survolee = -1
    End Sub

#End Region

#Region "Geometrie"

    'Hauteur d'une ligne : elle suit la hauteur de la zone, entre 14 et 34 pixels
    Private Shared Function HauteurLigne(zone As Rectangle) As Integer
        Return Math.Max(14, Math.Min(34, zone.Height \ LignesCibles))
    End Function

    'Quelle ligne se trouve a cette position de l'ecran ? (-1 si aucune)
    Private Function IndexA(x As Single, y As Single) As Integer
        If Not _derniereZone.Contains(CInt(x), CInt(y)) Then
            Return -1
        End If
        Dim index As Integer = _premiere + CInt(Math.Floor((y - _derniereZone.Top) / HauteurLigne(_derniereZone)))
        If index < 0 OrElse index >= Total Then
            Return -1
        End If
        Return index
    End Function

#End Region

#Region "Contrat IRenduBureau"

    ''' <summary>Dessine uniquement les lignes visibles dans zoneVisible.</summary>
    Public Sub Dessiner(g As Graphics, zoneVisible As Rectangle) Implements IRenduBureau.Dessiner
        _derniereZone = zoneVisible
        Dim total As Integer = Me.Total
        If total = 0 OrElse zoneVisible.Width < 40 OrElse zoneVisible.Height < 40 Then
            Return
        End If

        Dim hl As Integer = HauteurLigne(zoneVisible)
        Dim nb As Integer = Math.Max(1, zoneVisible.Height \ hl)
        If _premiere > total - nb Then
            _premiere = Math.Max(0, total - nb)
        End If

        'La police et les marges suivent la hauteur d'une ligne
        Dim taille As Single = hl * 0.6F
        Dim largeurCar As Single = taille * 0.55F
        Dim xNumero As Single = zoneVisible.X + 6
        Dim xCode As Single = xNumero + largeurCar * 8 + 6
        Dim decalage As Single = (hl - taille * 1.2F) / 2.0F

        g.SetClip(zoneVisible)
        Using police As New Font("Consolas", taille, FontStyle.Regular, GraphicsUnit.Pixel)
            Using fondSurvol As New SolidBrush(Color.FromArgb(255, 238, 140))
                Using fondBonne As New SolidBrush(Color.FromArgb(160, 225, 160))
                    Using fondFausse As New SolidBrush(Color.FromArgb(248, 205, 205))

                        For i As Integer = 0 To nb - 1
                            Dim index As Integer = _premiere + i
                            If index >= total Then
                                Exit For
                            End If
                            Dim y As Single = zoneVisible.Top + i * hl

                            'Couleur de fond : bonne ligne (vert), survol (jaune), ligne deja essayee (rouge pale)
                            If index = _correcte Then
                                g.FillRectangle(fondBonne, zoneVisible.X, y, zoneVisible.Width, hl)
                            ElseIf index = _survolee Then
                                g.FillRectangle(fondSurvol, zoneVisible.X, y, zoneVisible.Width, hl)
                            ElseIf _fausses.Contains(index) Then
                                g.FillRectangle(fondFausse, zoneVisible.X, y, zoneVisible.Width, hl)
                            End If

                            Dim ligne As CodeLine = _bureau.ObtenirLigne(index)
                            Dim brosseNumero As Brush = Brushes.Gray
                            If _fausses.Contains(index) Then
                                brosseNumero = Brushes.Firebrick
                            End If
                            g.DrawString(ligne.NumeroLigne.ToString().PadLeft(7), police, brosseNumero, xNumero, y + decalage)
                            g.DrawString(ligne.Contenu, police, Brushes.Black, xCode, y + decalage)
                        Next

                    End Using
                End Using
            End Using
        End Using
        g.ResetClip()
    End Sub

    ''' <summary>Renvoie la ligne situee a la position (x, y) de l'ecran, ou Nothing.</summary>
    Public Function ObtenirLigneALaPosition(x As Single, y As Single) As CodeLine _
        Implements IRenduBureau.ObtenirLigneALaPosition
        Dim index As Integer = IndexA(x, y)
        If index < 0 Then
            Return Nothing
        End If
        Return _bureau.ObtenirLigne(index)
    End Function

#End Region

End Class
