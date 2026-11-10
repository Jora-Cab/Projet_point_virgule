Option Strict On
Option Explicit On

Imports System.Diagnostics

''' <summary>
''' Chef d'orchestre du jeu (Etudiant 5) :
''' - boucle principale (un Timer qui declenche ImageSuivante toutes les 16 ms) ;
''' - chronometre et comptage des tentatives ;
''' - verification de la ligne choisie et condition de victoire.
''' </summary>
Public Class MoteurJeu
    Implements IDisposable

    Private ReadOnly _jeu As CodeJeu
    Private ReadOnly _distinctes As New HashSet(Of Integer)
    Private WithEvents _boucle As New System.Windows.Forms.Timer()
    Private _nbTentatives As Integer
    Private _partieEnCours As Boolean
    Private _libere As Boolean

    Public ReadOnly Property Bureau As Bureau
    Public ReadOnly Property Joueur As Joueur
    Public ReadOnly Property Chronometre As Stopwatch

    ''' <summary>Nombre de verifications faites par le joueur (une ligne choisie 2 fois compte 2 fois).</summary>
    Public ReadOnly Property NbTentatives As Integer
        Get
            Return _nbTentatives
        End Get
    End Property

    ''' <summary>Nombre de lignes differentes inspectees.</summary>
    Public ReadOnly Property NbLignesDistinctes As Integer
        Get
            Return _distinctes.Count
        End Get
    End Property

    Public ReadOnly Property PartieEnCours As Boolean
        Get
            Return _partieEnCours
        End Get
    End Property

    Public ReadOnly Property TempsEcoule As TimeSpan
        Get
            Return Chronometre.Elapsed
        End Get
    End Property

    ''' <summary>Declenche a chaque tour de la boucle principale (environ 60 fois par seconde).</summary>
    Public Event ImageSuivante()

    ''' <summary>Declenche a chaque ligne verifiee : bonne ou mauvaise (sert au retour visuel et sonore).</summary>
    Public Event LigneVerifiee(ligne As CodeLine, estCorrecte As Boolean)

    ''' <summary>Declenche quand le joueur a trouve la ligne fautive.</summary>
    Public Event PartieTerminee(tempsEcoule As TimeSpan, nbTentatives As Integer)

    Public Sub New(jeu As CodeJeu, bureau As Bureau, joueur As Joueur)
        If jeu Is Nothing Then
            Throw New ArgumentNullException("jeu")
        End If
        If bureau Is Nothing Then
            Throw New ArgumentNullException("bureau")
        End If
        If joueur Is Nothing Then
            Throw New ArgumentNullException("joueur")
        End If

        _jeu = jeu
        Me.Bureau = bureau
        Me.Joueur = joueur
        Chronometre = New Stopwatch()

        _boucle.Interval = 16
        _boucle.Start()
    End Sub

    'La boucle principale : un tour toutes les 16 ms
    Private Sub _boucle_Tick(sender As Object, e As EventArgs) Handles _boucle.Tick
        RaiseEvent ImageSuivante()
    End Sub

    ''' <summary>Nouvelle partie : nouvelle erreur, lignes reparties sur les murs, essais et chrono remis a zero.</summary>
    Public Sub DemarrerPartie()
        If _jeu.PartieEnCours Then
            _jeu.TerminerPartie()
        End If
        _jeu.DemarrerPartie()
        Bureau.RepartirLignes(_jeu.Courant)

        _distinctes.Clear()
        _nbTentatives = 0
        _partieEnCours = True
        Chronometre.Reset()
        Chronometre.Start()
    End Sub

    ''' <summary>Le joueur designe une ligne : renvoie True si c'est la bonne (fin de partie).</summary>
    Public Function VerifierVictoire(ligne As CodeLine) As Boolean
        If ligne Is Nothing Then
            Throw New ArgumentNullException("ligne")
        End If
        If Not _partieEnCours Then
            Return False
        End If

        _nbTentatives += 1
        _distinctes.Add(ligne.NumeroLigne)
        Dim estCorrecte As Boolean = Joueur.Inspecter(ligne)

        If estCorrecte Then
            Chronometre.Stop()
            _partieEnCours = False
        End If

        RaiseEvent LigneVerifiee(ligne, estCorrecte)
        If estCorrecte Then
            RaiseEvent PartieTerminee(Chronometre.Elapsed, _nbTentatives)
        End If
        Return estCorrecte
    End Function

    ''' <summary>Fin de partie : arrete le chrono et supprime la trace de la partie sur le disque.</summary>
    Public Sub TerminerPartie()
        Chronometre.Stop()
        _partieEnCours = False
        If _jeu.PartieEnCours Then
            _jeu.TerminerPartie()
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If _libere Then
            Return
        End If
        _libere = True
        _boucle.Stop()
        TerminerPartie()
        _boucle.Dispose()
    End Sub

End Class
