Option Strict On
Option Explicit On

Imports System.Diagnostics
Imports System.IO
Imports System.Media
Imports System.Text

''' <summary>
''' Sons du jeu : un son pour une bonne ligne, un autre pour une mauvaise ligne, et une ambiance de fond.
''' Les sons sont fabriques par le programme lui-meme : aucun fichier a ajouter au projet.
''' - Effets : joues par MCI (winmm), ce qui permet de les superposer a l'ambiance.
''' - Ambiance : SoundPlayer en boucle, depuis la memoire.
''' Si le son est indisponible sur la machine, le jeu continue sans son (aucune erreur).
''' </summary>
Public NotInheritable Class GestionSons
    Implements IDisposable

    Private Declare Auto Function mciSendString Lib "winmm.dll" (ByVal commande As String,
        ByVal retour As StringBuilder, ByVal tailleRetour As Integer, ByVal fenetre As IntPtr) As Integer

    Private Const Frequence As Integer = 22050
    Private Const AliasSucces As String = "pv_succes"
    Private Const AliasErreur As String = "pv_erreur"

    Private _fond As SoundPlayer
    Private _disponible As Boolean
    Private _actif As Boolean = True
    Private _fondDemande As Boolean
    Private _libere As Boolean

    Public Sub New()
        Try
            Dim dossier As String = Path.Combine(Path.GetTempPath(), "PointVirguleManquant")
            Directory.CreateDirectory(dossier)

            Dim cheminSucces As String = Path.Combine(dossier, "succes.wav")
            Dim cheminErreur As String = Path.Combine(dossier, "erreur.wav")
            File.WriteAllBytes(cheminSucces, VersWav(FabriquerSucces()))
            File.WriteAllBytes(cheminErreur, VersWav(FabriquerErreur()))

            Ouvrir(AliasSucces, cheminSucces)
            Ouvrir(AliasErreur, cheminErreur)

            _fond = New SoundPlayer(New MemoryStream(VersWav(FabriquerFond())))
            _disponible = True
        Catch ex As Exception
            Debug.WriteLine("Sons indisponibles : " & ex.Message)
            _disponible = False
        End Try
    End Sub

#Region "Commandes"

    ''' <summary>Son active ou coupe (touche P dans le jeu).</summary>
    Public Property Actif As Boolean
        Get
            Return _actif
        End Get
        Set(value As Boolean)
            _actif = value
            If Not value Then
                StopperFond()
            ElseIf _fondDemande Then
                LancerFond()
            End If
        End Set
    End Property

    Public Sub DemarrerFond()
        _fondDemande = True
        If _actif Then
            LancerFond()
        End If
    End Sub

    Public Sub ArreterFond()
        _fondDemande = False
        StopperFond()
    End Sub

    ''' <summary>Son de la bonne ligne : petit arpege qui monte.</summary>
    Public Sub JouerSucces()
        Jouer(AliasSucces)
    End Sub

    ''' <summary>Son de la mauvaise ligne : buzzer grave qui descend.</summary>
    Public Sub JouerErreur()
        Jouer(AliasErreur)
    End Sub

    Private Sub Jouer(nomAlias As String)
        If Not _disponible OrElse Not _actif Then
            Return
        End If
        mciSendString("play " & nomAlias & " from 0", Nothing, 0, IntPtr.Zero)
    End Sub

    Private Sub Ouvrir(nomAlias As String, chemin As String)
        mciSendString("close " & nomAlias, Nothing, 0, IntPtr.Zero)
        mciSendString("open """ & chemin & """ type waveaudio alias " & nomAlias, Nothing, 0, IntPtr.Zero)
    End Sub

    Private Sub LancerFond()
        If Not _disponible Then
            Return
        End If
        Try
            _fond.PlayLooping()
        Catch ex As Exception
            Debug.WriteLine("Ambiance impossible : " & ex.Message)
        End Try
    End Sub

    Private Sub StopperFond()
        If Not _disponible Then
            Return
        End If
        Try
            _fond.Stop()
        Catch ex As Exception
            Debug.WriteLine("Arret de l'ambiance impossible : " & ex.Message)
        End Try
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If _libere Then
            Return
        End If
        _libere = True
        Try
            If _fond IsNot Nothing Then
                _fond.Stop()
                _fond.Dispose()
            End If
            mciSendString("close all", Nothing, 0, IntPtr.Zero)
        Catch ex As Exception
            Debug.WriteLine("Liberation des sons : " & ex.Message)
        End Try
    End Sub

#End Region

#Region "Fabrication des sons"

    'Succes : 4 notes (do, mi, sol, do aigu) qui s'enchainent et s'eteignent doucement
    Private Shared Function FabriquerSucces() As Double()
        Dim notes As Double() = {523.25, 659.25, 783.99, 1046.5}
        Dim departs As Double() = {0.0, 0.09, 0.18, 0.27}
        Dim n As Integer = CInt(0.95 * Frequence)
        Dim son(n - 1) As Double

        For k As Integer = 0 To notes.Length - 1
            Dim debut As Integer = CInt(departs(k) * Frequence)
            Dim f As Double = notes(k)
            For i As Integer = debut To n - 1
                Dim t As Double = (i - debut) / CDbl(Frequence)
                Dim enveloppe As Double = (1.0 - Math.Exp(-t * 500.0)) * Math.Exp(-t * 6.5)
                'Attention : l'enveloppe vaut 0 a t = 0, on ne s'arrete qu'apres le debut de la note
                If t > 0.05 AndAlso enveloppe < 0.0001 Then
                    Exit For
                End If
                Dim v As Double = Math.Sin(2.0 * Math.PI * f * t) +
                                  0.35 * Math.Sin(2.0 * Math.PI * 2.0 * f * t) +
                                  0.12 * Math.Sin(2.0 * Math.PI * 3.0 * f * t)
                son(i) += v * enveloppe * 0.3
            Next
        Next
        Return son
    End Function

    'Erreur : buzzer grave dont la hauteur descend, avec un trémolo
    Private Shared Function FabriquerErreur() As Double()
        Dim n As Integer = CInt(0.5 * Frequence)
        Dim son(n - 1) As Double
        Dim phase As Double = 0.0

        For i As Integer = 0 To n - 1
            Dim t As Double = i / CDbl(Frequence)
            Dim f As Double = 120.0 - 45.0 * t / 0.5
            phase += 2.0 * Math.PI * f / Frequence

            Dim s As Double = 0.0
            For h As Integer = 1 To 6
                s += Math.Sin(h * phase) / h
            Next

            Dim tremolo As Double = 0.75 + 0.25 * Math.Sin(2.0 * Math.PI * 28.0 * t)
            Dim enveloppe As Double = 1.0
            If t < 0.006 Then
                enveloppe = t / 0.006
            ElseIf t > 0.32 Then
                enveloppe = Math.Max(0.0, 1.0 - (t - 0.32) / 0.18)
            End If

            son(i) = s * tremolo * enveloppe * 0.42
        Next
        Return son
    End Function

    'Ambiance de bureau : ventilation, bourdonnement, nappe grave et clics de clavier (boucle de 16 s)
    Private Shared Function FabriquerFond() As Double()
        Dim longueur As Integer = 16 * Frequence
        Dim fondu As Integer = Frequence
        Dim n As Integer = longueur + fondu
        Dim alea As New Random(12345)

        'Ventilation : bruit blanc adouci deux fois
        Dim bruit(n - 1) As Double
        Dim a As Double = 0.0
        Dim b As Double = 0.0
        For i As Integer = 0 To n - 1
            Dim x As Double = alea.NextDouble() * 2.0 - 1.0
            a += 0.015 * (x - a)
            b += 0.03 * (a - b)
            bruit(i) = b
        Next

        'Clics de clavier : de tres courts bruits places au hasard
        Dim clics(n - 1) As Double
        Dim nbClics As Integer = CInt(n / CDbl(Frequence) * 1.6)
        For c As Integer = 1 To nbClics
            Dim position As Integer = alea.Next(0, n - 400)
            Dim amplitude As Double = 0.03 + alea.NextDouble() * 0.08
            Dim precedent As Double = 0.0
            For j As Integer = 0 To 259
                Dim y As Double = alea.NextDouble() * 2.0 - 1.0
                clics(position + j) += (y - precedent) * amplitude * Math.Exp(-j / 45.0)
                precedent = y
            Next
        Next

        'Melange de toutes les couches (le volume reste bas : c'est une ambiance)
        Dim son(n - 1) As Double
        For i As Integer = 0 To n - 1
            Dim t As Double = i / CDbl(Frequence)
            Dim respiration As Double = 0.8 + 0.2 * Math.Sin(2.0 * Math.PI * 0.25 * t)
            Dim bourdon As Double = 0.02 * Math.Sin(2.0 * Math.PI * 55.0 * t) +
                                    0.012 * Math.Sin(2.0 * Math.PI * 110.0 * t)
            Dim nappe As Double = 0.018 * Math.Sin(2.0 * Math.PI * 110.0 * t + 0.4) +
                                  0.012 * Math.Sin(2.0 * Math.PI * 165.0 * t) +
                                  0.008 * Math.Sin(2.0 * Math.PI * 220.0 * t)
            Dim lent As Double = 0.6 + 0.4 * Math.Sin(2.0 * Math.PI * 0.125 * t)
            son(i) = (bruit(i) * 3.2 * respiration + bourdon + nappe * lent + clics(i)) * 0.4
        Next

        'Boucle sans couture : fondu enchaine entre la fin et le debut
        Dim boucle(longueur - 1) As Double
        For i As Integer = 0 To longueur - 1
            If i < fondu Then
                Dim w As Double = i / CDbl(fondu)
                boucle(i) = son(i) * w + son(longueur + i) * (1.0 - w)
            Else
                boucle(i) = son(i)
            End If
        Next
        Return boucle
    End Function

    'Transforme des echantillons (de -1 a 1) en fichier WAV (16 bits, mono) en memoire
    Private Shared Function VersWav(echantillons As Double()) As Byte()
        Dim n As Integer = echantillons.Length

        Using flux As New MemoryStream()
            Using ecrivain As New BinaryWriter(flux)
                ecrivain.Write(Encoding.ASCII.GetBytes("RIFF"))
                ecrivain.Write(36 + n * 2)
                ecrivain.Write(Encoding.ASCII.GetBytes("WAVE"))
                ecrivain.Write(Encoding.ASCII.GetBytes("fmt "))
                ecrivain.Write(16)
                ecrivain.Write(CShort(1))
                ecrivain.Write(CShort(1))
                ecrivain.Write(Frequence)
                ecrivain.Write(Frequence * 2)
                ecrivain.Write(CShort(2))
                ecrivain.Write(CShort(16))
                ecrivain.Write(Encoding.ASCII.GetBytes("data"))
                ecrivain.Write(n * 2)

                For i As Integer = 0 To n - 1
                    Dim v As Double = Math.Max(-1.0, Math.Min(1.0, echantillons(i)))
                    ecrivain.Write(CShort(Math.Round(v * 32767.0)))
                Next
                ecrivain.Flush()
            End Using
            Return flux.ToArray()
        End Using
    End Function

#End Region

End Class
