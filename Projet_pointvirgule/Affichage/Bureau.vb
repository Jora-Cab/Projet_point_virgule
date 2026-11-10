Option Strict On
Option Explicit On

''' <summary>
''' Le bureau : 4 murs couverts de lignes de code.
''' Il stocke et repartit les lignes. Le dessin est fait par VueJeu (3D) et ZoneCode (lecture du code).
''' </summary>
Public Class Bureau

    Private ReadOnly _murs As New List(Of Mur)
    Private ReadOnly _alea As New Random()

    Public ReadOnly Property Murs As IReadOnlyList(Of Mur)
        Get
            Return _murs
        End Get
    End Property

    ''' <summary>Nombre total de lignes de code sur les 4 murs.</summary>
    Public ReadOnly Property NombreLignes As Integer
        Get
            Dim total As Integer = 0
            For Each unMur As Mur In _murs
                total += unMur.Lignes.Count
            Next
            Return total
        End Get
    End Property

    Public Sub New()
        CreerMurs()
    End Sub

    'Cree 4 murs vides (efface les anciens s'il y en avait)
    Private Sub CreerMurs()
        _murs.Clear()
        _murs.Add(New Mur(Orientation.Nord))
        _murs.Add(New Mur(Orientation.Est))
        _murs.Add(New Mur(Orientation.Sud))
        _murs.Add(New Mur(Orientation.Ouest))
    End Sub

    ''' <summary>Genere les lignes avec un generateur, puis les repartit sur les 4 murs.</summary>
    Public Sub GenererContenu(generateur As IGenerateurCode, nombreLignes As Integer)
        If generateur Is Nothing Then
            Throw New ArgumentNullException("generateur")
        End If
        If nombreLignes < 1 Then
            Throw New ArgumentOutOfRangeException("nombreLignes")
        End If
        RepartirLignes(generateur.GenererLignesValides(nombreLignes))
    End Sub

    ''' <summary>Repartit des lignes deja pretes (venant de CodeJeu) sur les 4 murs, en blocs consecutifs.</summary>
    Public Sub RepartirLignes(lignes As IReadOnlyList(Of CodeLine))
        If lignes Is Nothing Then
            Throw New ArgumentNullException("lignes")
        End If
        If lignes.Count = 0 Then
            Throw New ArgumentException("Aucune ligne a repartir", "lignes")
        End If

        CreerMurs()

        Dim lignesParMur As Integer = CInt(Math.Ceiling(lignes.Count / _murs.Count))
        For i As Integer = 0 To lignes.Count - 1
            _murs(i \ lignesParMur).AjouterLigne(lignes(i))
        Next
    End Sub

    ''' <summary>
    ''' Place une erreur : tire une ligne au hasard et la remplace par sa version cassee.
    ''' Si la ligne ne peut pas etre cassee (accolade, commentaire...), on en essaie une autre.
    ''' </summary>
    Public Sub PlacerErreur(generateur As IGenerateurCode)
        If generateur Is Nothing Then
            Throw New ArgumentNullException("generateur")
        End If
        Dim total As Integer = NombreLignes
        If total = 0 Then
            Throw New InvalidOperationException("Appeler GenererContenu ou RepartirLignes avant PlacerErreur")
        End If

        For essai As Integer = 1 To 500
            Dim leMur As Mur = Nothing
            Dim decalage As Integer = 0
            If Localiser(_alea.Next(total), leMur, decalage) Then
                Try
                    Dim cassee As CodeLine = generateur.CasserLigne(leMur.Lignes(decalage))
                    leMur.RemplacerLigne(decalage, cassee)
                    Return
                Catch ex As InvalidOperationException
                    'Cette ligne ne peut pas etre cassee : on tire un autre numero
                End Try
            End If
        Next
        Throw New InvalidOperationException("Aucune ligne cassable n'a ete trouvee.")
    End Sub

    ''' <summary>Renvoie la ligne numero "index" (de 0 a NombreLignes - 1), tous murs confondus.</summary>
    Public Function ObtenirLigne(index As Integer) As CodeLine
        Dim leMur As Mur = Nothing
        Dim decalage As Integer = 0
        If index < 0 OrElse Not Localiser(index, leMur, decalage) Then
            Throw New ArgumentOutOfRangeException("index")
        End If
        Return leMur.Lignes(decalage)
    End Function

    'Trouve sur quel mur se trouve la ligne "index", et a quelle place sur ce mur
    Private Function Localiser(index As Integer, ByRef leMur As Mur, ByRef decalage As Integer) As Boolean
        For Each unMur As Mur In _murs
            If index < unMur.Lignes.Count Then
                leMur = unMur
                decalage = index
                Return True
            End If
            index -= unMur.Lignes.Count
        Next
        Return False
    End Function

End Class
