Option Strict On
Option Explicit On

''' <summary>
''' Fenetre principale : assemble le code de l'Etudiant 2 (CodeJeu), le moteur (Etudiant 5),
''' le joueur et le retour sonore (Etudiant 4), et l'affichage (Etudiant 3).
''' Touches : fleches ou Z Q S D (marcher), R / F (regarder haut / bas), Home (regard a plat),
''' E ou clic droit (loupe), clic gauche (feuille / ligne), Entree (lire en grand), Echap (retour),
''' M (souris), P (son), F12 (tests).
''' </summary>
Public Class Form1

    Private _jeu As CodeJeu
    Private _moteur As MoteurJeu
    Private _joueur As Joueur
    Private _bureau As Bureau
    Private _zone As ZoneCode
    Private _sons As GestionSons
    Private _retour As RetourSensoriel
    Private _vue As VueJeu
    Private _lecture As VueLecture

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "Le Point-Virgule Manquant"
        Me.ClientSize = New Size(960, 540)
        Me.MinimumSize = New Size(700, 450)

        Dim plan As Carte = Carte.CreerParDefaut()
        _joueur = New Joueur()
        _bureau = New Bureau()
        _zone = New ZoneCode(_bureau)

        'Le code du jeu (Etudiant 2) : charge du disque, ou genere la toute premiere fois
        _jeu = New CodeJeu(SauvegardeCode.DossierParDefaut(), GenerateurCode.NombreLignesParDefaut)

        'Le moteur (Etudiant 5), les sons et le retour visuel (Etudiant 4)
        _moteur = New MoteurJeu(_jeu, _bureau, _joueur)
        _sons = New GestionSons()
        _retour = New RetourSensoriel(_moteur, _zone, _sons)

        'La vue 3D du bureau
        _vue = New VueJeu()
        _vue.Dock = DockStyle.Fill
        _vue.Configurer(plan, _joueur, _bureau)
        _vue.Moteur = _moteur
        _vue.Zone = _zone

        'La page de lecture (cachee au depart)
        _lecture = New VueLecture()
        _lecture.Dock = DockStyle.Fill
        _lecture.Configurer(_zone)
        _lecture.Visible = False

        'Les evenements : la boucle du moteur anime la vue, les clics arrivent au moteur
        AddHandler _moteur.ImageSuivante, AddressOf _vue.Actualiser
        AddHandler _moteur.PartieTerminee, AddressOf OnPartieTerminee
        AddHandler _vue.FeuilleOuverte, AddressOf OnFeuilleOuverte
        AddHandler _vue.LigneCliquee, AddressOf OnLigneChoisie
        AddHandler _lecture.LigneChoisie, AddressOf OnLigneChoisie

        Me.Controls.Add(_lecture)
        Me.Controls.Add(_vue)

        NouvellePartie()
    End Sub

    Private Sub NouvellePartie()
        _moteur.DemarrerPartie()
        _zone.Reinitialiser()
        _joueur.PlacerEn(4.5F, 4.5F, CSng(Math.PI / 2.0))

        _lecture.Visible = False
        _vue.Visible = True
        _vue.EnPause = False
        _vue.Focus()
        _sons.DemarrerFond()
    End Sub

    'Le joueur veut lire la feuille en grand : on affiche la page de lecture
    Private Sub OnFeuilleOuverte(nom As String, debut As Integer)
        _vue.EnPause = True
        _vue.Visible = False
        _lecture.Ouvrir(nom)
        _lecture.Visible = True
        _lecture.Focus()
    End Sub

    Private Sub FermerLecture()
        _lecture.Visible = False
        _vue.Visible = True
        _vue.EnPause = False
        _vue.Focus()
    End Sub

    'Le joueur a clique sur une ligne (page de lecture ou feuille en gros plan) : le moteur verifie.
    'Le retour visuel et sonore est declenche par l'evenement LigneVerifiee du moteur.
    Private Sub OnLigneChoisie(ligne As CodeLine)
        _moteur.VerifierVictoire(ligne)
    End Sub

    Private Sub OnPartieTerminee(tempsEcoule As TimeSpan, nbTentatives As Integer)
        _sons.ArreterFond()
        _vue.Invalidate()
        _lecture.Invalidate()

        Dim message As String = "Bravo ! Le point-virgule manquant était à la ligne " &
                                _jeu.LigneErreur.NumeroLigne.ToString() & "." & vbCrLf & vbCrLf &
                                "Temps : " & CInt(Math.Floor(tempsEcoule.TotalMinutes)).ToString("00") & ":" &
                                tempsEcoule.Seconds.ToString("00") & vbCrLf &
                                "Essais : " & nbTentatives.ToString() & vbCrLf &
                                "Lignes différentes inspectées : " & _moteur.NbLignesDistinctes.ToString() &
                                vbCrLf & vbCrLf & "Rejouer ?"

        If MessageBox.Show(message, "Victoire", MessageBoxButtons.YesNo,
                           MessageBoxIcon.Information) = DialogResult.Yes Then
            NouvellePartie()
        Else
            Me.Close()
        End If
    End Sub

    Private Sub OuvrirTests()
        Using fenetre As New FormTests()
            fenetre.ShowDialog(Me)
        End Using
    End Sub

    'Clavier : raccourcis du jeu et defilement de la page de lecture
    Protected Overrides Function ProcessCmdKey(ByRef msg As System.Windows.Forms.Message,
                                               keyData As System.Windows.Forms.Keys) As Boolean
        'Bit 30 du message : la touche est deja enfoncee (repetition automatique)
        Dim repetition As Boolean = (msg.LParam.ToInt64() And &H40000000L) <> 0

        If keyData = Keys.F12 Then
            If Not repetition Then
                OuvrirTests()
            End If
            Return True
        End If

        If _moteur Is Nothing Then
            Return MyBase.ProcessCmdKey(msg, keyData)
        End If

        If keyData = Keys.P Then
            If Not repetition Then
                _sons.Actif = Not _sons.Actif
            End If
            Return True
        End If

        If keyData = Keys.M Then
            If Not repetition Then
                _vue.SourisActive = Not _vue.SourisActive
            End If
            Return True
        End If

        If _lecture.Visible Then
            If keyData = Keys.Escape Then
                FermerLecture()
                Return True
            ElseIf keyData = Keys.Up Then
                _lecture.Defiler(-1)
                Return True
            ElseIf keyData = Keys.Down Then
                _lecture.Defiler(1)
                Return True
            ElseIf keyData = Keys.PageUp Then
                _lecture.Defiler(-20)
                Return True
            ElseIf keyData = Keys.PageDown Then
                _lecture.Defiler(20)
                Return True
            End If
        ElseIf keyData = Keys.Enter Then
            _vue.OuvrirFeuilleVisee()
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub Form1_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        If _retour IsNot Nothing Then
            _retour.Dispose()
        End If
        If _sons IsNot Nothing Then
            _sons.Dispose()
        End If
        If _moteur IsNot Nothing Then
            _moteur.Dispose()
        End If
    End Sub

End Class
