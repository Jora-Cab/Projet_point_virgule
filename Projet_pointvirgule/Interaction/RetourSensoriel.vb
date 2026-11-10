Option Strict On
Option Explicit On

''' <summary>
''' Retour visuel et sonore quand le joueur inspecte une ligne (Etudiant 4).
''' Il ecoute le moteur : a chaque verification, il colore la ligne (vert = bonne, rouge = fausse),
''' ecrit un message et joue le son correspondant.
''' </summary>
Public NotInheritable Class RetourSensoriel
    Implements IDisposable

    Private ReadOnly _moteur As MoteurJeu
    Private ReadOnly _zone As ZoneCode
    Private ReadOnly _sons As GestionSons

    Public Sub New(moteur As MoteurJeu, zone As ZoneCode, sons As GestionSons)
        If moteur Is Nothing Then
            Throw New ArgumentNullException("moteur")
        End If
        If zone Is Nothing Then
            Throw New ArgumentNullException("zone")
        End If
        If sons Is Nothing Then
            Throw New ArgumentNullException("sons")
        End If

        _moteur = moteur
        _zone = zone
        _sons = sons
        AddHandler _moteur.LigneVerifiee, AddressOf OnLigneVerifiee
    End Sub

    Private Sub OnLigneVerifiee(ligne As CodeLine, estCorrecte As Boolean)
        'Le numero de ligne commence a 1, l'index dans la liste commence a 0
        _zone.Marquer(ligne.NumeroLigne - 1, estCorrecte)

        If estCorrecte Then
            _zone.MessageReussite = True
            _zone.Message = "Trouvé ! Le point-virgule manquait à la ligne " & ligne.NumeroLigne.ToString() & "."
            _sons.JouerSucces()
        Else
            _zone.MessageReussite = False
            _zone.Message = "Ligne " & ligne.NumeroLigne.ToString() & " : rien ne manque ici.   Essai n° " &
                            _moteur.NbTentatives.ToString()
            _sons.JouerErreur()
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        RemoveHandler _moteur.LigneVerifiee, AddressOf OnLigneVerifiee
    End Sub

End Class
