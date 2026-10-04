Option Strict On
Option Explicit On

Imports System.Diagnostics

''' <summary>
''' C'est ici que demarre la partie, qu'on compte les tentatives, detecte la victoire
''' </summary>

Public Class MoteurJeu
    'Attribut de la classe moteur jeu
    Private ReadOnly _generateur As IGenerateurCode
    Private _nbTentatives As Integer

    Public ReadOnly Property Bureau As Bureau
    Public ReadOnly Property joueur As Joueur
    Public Property Chronometre As Stopwatch

    Public ReadOnly Property NbTentatives As Integer
        Get
            Return _nbTentatives
        End Get
    End Property

    'Evenement declenche lorsque le joueur trouve la ligne fautive
    Public Event PartieTerminer(tempsEcoule As TimeSpan, nbTentatives As Integer)

    'Constructeur de la methode Moteur de jeu
    Public Sub New(generateur As IGenerateurCode, bureau As Bureau, joueur As J)
        If generateur Is Nothing Then
            Throw New ArgumentNullException("generateur")
        End If
        If bureau Is Nothing Then
            Throw New ArgumentNullException("bureau")
        End If
        If joueur Is Nothing Then
            Throw New ArgumentNullException("joueur")
        End If

        _generateur = generateur
        Me.Bureau = bureau
        Me.joueur = joueur
        Chronometre = New Stopwatch()
    End Sub

    'Methode de la classe moteur jeu pour demarrer une partie
    Public Sub DemarrerPartie()
        'Etudiant 5 : Generer le contenu, placer l'erreur
        'Remise a zero des tentatives, demarrer le chronometre
        Throw New NotImplementedException("A faire par l'etudiant 5")
    End Sub

    'Methode de la classe Verifier Victoire
    Public Function VerifierVictoire(ligne As CodeLine) As Boolean
        'Etudiant 5 : compter la tentative, appeler joueur.inspecter
        'arreter le chonometre, declencher PartieTerminee
        Throw New NotImplementedException("A faire par l'etudiant 5")
    End Function

End Class
