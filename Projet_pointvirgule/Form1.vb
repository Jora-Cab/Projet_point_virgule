Public Class Form1

    Private _jeu As CodeJeu
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.ClientSize = New Size(960, 540)

        Dim plan As Carte = Carte.CreerParDefaut()
        Dim monJoueur As New Joueur()
        monJoueur.PlacerEn(4.5F, 4.5F, CSng(Math.PI / 2.0))

        'Code de l'etudiant 2
        _jeu = New CodeJeu(SauvegardeCode.DossierParDefaut(), GenerateurCode.NombreLignesParDefaut)
        _jeu.DemarrerPartie()

        Dim monBureau As New Bureau()
        monBureau.RepartirLignes(_jeu.Courant)


        Dim vue As New VueJeu()
        vue.Dock = DockStyle.Fill
        vue.Configurer(plan, monJoueur, monBureau)
        Me.Controls.Add(vue)
    End Sub

    Private Sub Form1_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed

        If _jeu IsNot Nothing Then
            _jeu.TerminerPartie()
        End If

    End Sub

End Class