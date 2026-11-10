'Option Strict On
'Option Explicit On

'Imports System.Windows.Forms

'''' <summary>
'''' Formulaire principal : héberge VueJeu, démarre la partie via MoteurJeu,
'''' relaie les actions et gère les retours sonores et la fin de partie.
'''' </summary>
'Public Class FormJeu
'    Inherits Form

'    Private WithEvents _vue As New VueJeu()
'    Private WithEvents _moteur As MoteurJeu
'    Private _sons As GestionnaireSon

'    Public Sub New()
'        Me.Text = "Le Point-Virgule Manquant"
'        Me.WindowState = FormWindowState.Maximized
'        Me.DoubleBuffered = True

'        _vue.Dock = DockStyle.Fill
'        Me.Controls.Add(_vue)
'    End Sub

'    Private Sub FormJeu_Load(sender As Object, e As EventArgs) Handles Me.Load
'        Dim carte As Carte = Carte.CreerParDefaut()
'        Dim joueur As New Joueur()
'        joueur.PlacerEn(1.5F, 1.5F, 0.0F)

'        Dim bureau As New Bureau()

'        ' Utilisation d'un adaptateur ou d'une classe conforme à IGenerateurCode 
'        ' pour relier GenerateurCode de l'étudiant 2 au MoteurJeu de l'étudiant 5.
'        Dim generateur As IGenerateurCode = New AdaptateurGenerateurC()

'        _moteur = New MoteurJeu(generateur, bureau, joueur)
'        _sons = New GestionnaireSon()

'        _vue.Configurer(carte, joueur, bureau)
'        _vue.Moteur = _moteur

'        _moteur.DemarrerPartie()
'        _sons.DemarrerMusiqueFond()
'    End Sub

'    Private Sub Vue_LigneInspectee(sender As Object, ligne As CodeLine) Handles _vue.LigneInspectee
'        If _moteur Is Nothing OrElse Not _moteur.EnPartie Then
'            Return
'        End If

'        Dim correcte As Boolean = _moteur.VerifierVictoire(ligne)

'        If correcte Then
'            _sons.JouerLigneCorrecte()
'        Else
'            _sons.JouerLigneIncorrecte()
'        End If

'        _vue.AfficherResultatInspection(correcte)
'    End Sub

'    Private Sub Moteur_PartieTerminer(tempsEcoule As TimeSpan, nbTentatives As Integer) _
'        Handles _moteur.PartieTerminer
'        _sons.ArreterMusiqueFond()
'        MessageBox.Show(
'            $"Trouvé en {tempsEcoule:mm\:ss} et {nbTentatives} tentative(s) !",
'            "Victoire", MessageBoxButtons.OK, MessageBoxIcon.Information)
'    End Sub

'    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
'        _sons?.Dispose()
'        MyBase.OnFormClosed(e)
'    End Sub

'End Class

'''' <summary>
'''' Adaptateur permettant de faire correspondre les méthodes statiques de GenerateurCode 
'''' aux exigences de l'interface IGenerateurCode.
'''' </summary>
'Public Class AdaptateurGenerateurC
'    Implements IGenerateurCode

'    Public Function GenererLignesValides(nombre As Integer) As List(Of CodeLine) Implements IGenerateurCode.GenererLignesValides
'        Return GenerateurCode.GenererLignesValides(nombre)
'    End Function

'    Public Function CasserLigne(ligne As CodeLine) As CodeLine Implements IGenerateurCode.CasserLigne
'        Return GenerateurCode.CasserLigne(ligne)
'    End Function
'End Class