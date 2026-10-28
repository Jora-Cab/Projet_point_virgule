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
    Public Sub New(generateur As IGenerateurCode, bureau As Bureau, joueur As Joueur)
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
        'Remettre le compteur à zéro
        _nbTentatives = 0

        ' Réinitialiser le chronomètre
        Chronometre.Reset()

        ' TODO : générer le contenu du jeu 
        ' et placer l'erreur avec _generateur et Bureau. 

        ' Démarrer le chronomètre
        Chronometre.Start()
        'Remise a zero des tentatives, demarrer le chronometre

    End Sub

    'Methode de la classe Verifier Victoire
    Public Function VerifierVictoire(ligne As CodeLine) As Boolean
        ' Vérifier que la ligne existe
        If ligne Is Nothing Then
            Throw New ArgumentNullException(NameOf(ligne))
        End If
        ' Compter la tentative
        _nbTentatives += 1
        ' verifier si le joueur a touver la ligne fautive
        Dim victoire As Boolean = joueur.Inspecter(ligne)

        'si le joueur a trouve la bonne ligne
        If victoire Then

            'arreter le chronometre
            Chronometre.Stop()

            'declencher l'evenement de fin de partie
            RaiseEvent PartieTerminer(
                Chronometre.Elapsed,
                _nbTentatives
                )
            Return True
        End If

        'si la ligne selectionnee n'est pas fautive
        Return False

    End Function

End Class
