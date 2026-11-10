Option Strict On
Option Explicit On

Imports System.IO
Imports System.Media
Imports System.Diagnostics

''' <summary>
''' Centralise les sons du jeu : bip correct/incorrect a l'inspection d'une ligne,
''' et musique d'ambiance en boucle pendant la partie. Etudiant 4 - feedback.
''' </summary>
Public Class GestionnaireSon
    Implements IDisposable

    Private ReadOnly _sonCorrect As SoundPlayer
    Private ReadOnly _sonIncorrect As SoundPlayer
    Private ReadOnly _musiqueFond As SoundPlayer
    Private _disposed As Boolean

    ''' <summary>dossierSons : dossier contenant les 3 .wav (par defaut "Sons" a cote de l'exe).</summary>
    Public Sub New(Optional dossierSons As String = "Sons")
        Dim racine As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dossierSons)

        _sonCorrect = ChargerSonSync(Path.Combine(racine, "son_ligne_correcte.wav"))
        _sonIncorrect = ChargerSonSync(Path.Combine(racine, "son_ligne_incorrecte.wav"))
        _musiqueFond = ChargerSonAsync(Path.Combine(racine, "musique_fond.wav"))
    End Sub

    Private Function ChargerSonSync(chemin As String) As SoundPlayer
        Dim lecteur As New SoundPlayer()
        Try
            If File.Exists(chemin) Then
                lecteur.SoundLocation = chemin
                lecteur.Load()
            End If
        Catch ex As Exception
            Debug.WriteLine("Son non charge (" & chemin & ") : " & ex.Message)
        End Try
        Return lecteur
    End Function

    Private Function ChargerSonAsync(chemin As String) As SoundPlayer
        Dim lecteur As New SoundPlayer()
        Try
            If File.Exists(chemin) Then
                lecteur.SoundLocation = chemin
                lecteur.LoadAsync()
            End If
        Catch ex As Exception
            Debug.WriteLine("Musique non chargee (" & chemin & ") : " & ex.Message)
        End Try
        Return lecteur
    End Function

    Public Sub JouerLigneCorrecte()
        JouerSansBloquer(_sonCorrect)
    End Sub

    Public Sub JouerLigneIncorrecte()
        JouerSansBloquer(_sonIncorrect)
    End Sub

    Private Sub JouerSansBloquer(lecteur As SoundPlayer)
        Try
            lecteur.Stop()
            lecteur.Play()
        Catch ex As Exception
            Debug.WriteLine("Lecture son impossible : " & ex.Message)
        End Try
    End Sub

    Public Sub DemarrerMusiqueFond()
        Try
            _musiqueFond.PlayLooping()
        Catch ex As Exception
            Debug.WriteLine("Musique de fond impossible : " & ex.Message)
        End Try
    End Sub

    Public Sub ArreterMusiqueFond()
        Try
            _musiqueFond.Stop()
        Catch ex As Exception
            Debug.WriteLine("Arret musique impossible : " & ex.Message)
        End Try
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If _disposed Then Return
        _disposed = True
        _sonCorrect.Dispose()
        _sonIncorrect.Dispose()
        _musiqueFond.Dispose()
    End Sub

End Class