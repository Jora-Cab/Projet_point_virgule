Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO

''' <summary>
''' Tests automatiques de l'Etudiant 5 : generation, detection de l'erreur, bureau, joueur, moteur.
''' Lancement : touche F12 dans le jeu (fenetre FormTests).
''' Un test est une methode qui leve une exception si quelque chose ne va pas.
''' </summary>
Public NotInheritable Class TestsEtudiant5

    Private Sub New()
    End Sub

    'Information supplementaire affichee a cote du resultat (ex : durees mesurees)
    Private Shared _detail As String = ""

    ''' <summary>Execute tous les tests et renvoie un resultat par test.</summary>
    Public Shared Function Executer() As List(Of ResultatTest)
        Dim resultats As New List(Of ResultatTest)

        'Generation et detection de l'erreur
        Lancer(resultats, "G01  Generation : 1000 lignes valides, numerotees de 1 a 1000", AddressOf T01_Generation)
        Lancer(resultats, "G02  Generation reproductible avec une graine fixe", AddressOf T02_Reproductible)
        Lancer(resultats, "G03  Il existe des lignes cassables parmi les lignes generees", AddressOf T03_Cassables)
        Lancer(resultats, "E01  InsererErreur : une seule erreur, bonne ligne cassee, les autres intactes", AddressOf T04_InsererErreur)
        Lancer(resultats, "E02  InsererErreur refuse une liste qui contient deja une erreur", AddressOf T05_DoubleErreur)
        Lancer(resultats, "E03  CasserLigne refuse une ligne qui ne peut pas etre cassee", AddressOf T06_LigneNonCassable)
        Lancer(resultats, "E04  Position de l'erreur aleatoire (60 parties, au moins 10 positions differentes)", AddressOf T07_Aleatoire)
        Lancer(resultats, "E05  CodeJeu : cycle d'une partie (demarrer, une erreur, terminer)", AddressOf T08_CycleCodeJeu)
        Lancer(resultats, "E06  CodeJeu : le programme sauvegarde est recharge a l'identique", AddressOf T09_Sauvegarde)

        'Bureau, murs, plan
        Lancer(resultats, "B01  Bureau : repartition des lignes sur les 4 murs", AddressOf T10_Bureau)
        Lancer(resultats, "B02  Mur : lignes visibles (limites, debut negatif, fin depassee)", AddressOf T11_MurVisible)
        Lancer(resultats, "B03  Carte : case hors du plan = mur, plan vide refuse", AddressOf T12_Carte)

        'Joueur
        Lancer(resultats, "J01  Joueur : Inspecter renvoie True seulement pour la ligne fautive", AddressOf T13_Inspecter)
        Lancer(resultats, "J02  Joueur : collisions avec les murs, les cloisons et les limites du plan", AddressOf T14_Collisions)
        Lancer(resultats, "J03  Joueur : l'angle reste entre -Pi et Pi", AddressOf T15_Rotation)

        'Moteur
        Lancer(resultats, "M01  Moteur : essais comptes, victoire detectee, chrono arrete, evenements", AddressOf T16_Moteur)

        'Performance
        Lancer(resultats, "P01  Performance : 187 421 lignes (generation, repartition, lecture)", AddressOf T17_Performance)

        Return resultats
    End Function

#Region "Outils"

    Private Shared Sub Lancer(resultats As List(Of ResultatTest), nom As String, test As Action)
        Dim r As New ResultatTest()
        r.Nom = nom
        _detail = ""

        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Try
            test()
            r.Reussi = True
        Catch ex As Exception
            r.Reussi = False
            r.Message = ex.Message
        End Try
        chrono.Stop()

        r.DureeMs = chrono.ElapsedMilliseconds
        r.Detail = _detail
        resultats.Add(r)
    End Sub

    Private Shared Sub Exiger(condition As Boolean, message As String)
        If Not condition Then
            Throw New InvalidOperationException(message)
        End If
    End Sub

    'Execute une action et renvoie l'exception qu'elle a levee (Nothing si elle n'en leve pas)
    Private Shared Function LeveException(action As Action) As Exception
        Try
            action()
        Catch ex As Exception
            Return ex
        End Try
        Return Nothing
    End Function

    Private Shared Function CompterErreurs(lignes As IEnumerable(Of CodeLine)) As Integer
        Dim n As Integer = 0
        For Each ligne As CodeLine In lignes
            If ligne.EstErreur Then
                n += 1
            End If
        Next
        Return n
    End Function

    Private Shared Function NouveauDossier() As String
        Return Path.Combine(Path.GetTempPath(), "pv_tests_" & Guid.NewGuid().ToString("N"))
    End Function

    Private Shared Sub Supprimer(dossier As String)
        Try
            If Directory.Exists(dossier) Then
                Directory.Delete(dossier, True)
            End If
        Catch ex As IOException
            Debug.WriteLine("Nettoyage impossible : " & ex.Message)
        End Try
    End Sub

    'Premiere ligne, a partir de "debut", qui n'est pas la ligne fautive
    Private Shared Function LigneSansErreur(bureau As Bureau, debut As Integer) As CodeLine
        For i As Integer = debut To bureau.NombreLignes - 1
            Dim ligne As CodeLine = bureau.ObtenirLigne(i)
            If Not ligne.EstErreur Then
                Return ligne
            End If
        Next
        Throw New InvalidOperationException("Aucune ligne sans erreur trouvee")
    End Function

    Private Shared Sub CreerCarteVide()
        Dim c As New Carte(New String() {})
    End Sub

#End Region

#Region "Generation et detection de l'erreur"

    Private Shared Sub T01_Generation()
        Dim lignes As List(Of CodeLine) = GenerateurCode.GenererLignesValides(1000)
        Exiger(lignes.Count = 1000, "Il devait y avoir 1000 lignes, il y en a " & lignes.Count.ToString())

        Dim nonVides As Integer = 0
        For i As Integer = 0 To lignes.Count - 1
            Exiger(lignes(i).NumeroLigne = i + 1, "Numerotation incorrecte a l'index " & i.ToString())
            Exiger(Not lignes(i).EstErreur, "La ligne " & (i + 1).ToString() & " est deja une erreur")
            If lignes(i).Contenu.Trim() <> "" Then
                nonVides += 1
            End If
        Next
        Exiger(nonVides > 0, "Toutes les lignes sont vides")
        _detail = nonVides.ToString() & " lignes non vides sur 1000"
    End Sub

    Private Shared Sub T02_Reproductible()
        Dim a As List(Of CodeLine) = GenerateurCode.GenererLignesValides(500, New Random(42))
        Dim b As List(Of CodeLine) = GenerateurCode.GenererLignesValides(500, New Random(42))
        Exiger(a.Count = b.Count, "Les deux generations n'ont pas la meme taille")
        For i As Integer = 0 To a.Count - 1
            Exiger(a(i).Contenu = b(i).Contenu, "Les lignes " & (i + 1).ToString() & " different avec la meme graine")
        Next
    End Sub

    Private Shared Sub T03_Cassables()
        Dim lignes As List(Of CodeLine) = GenerateurCode.GenererLignesValides(1000, New Random(1))
        Dim n As Integer = 0
        For Each ligne As CodeLine In lignes
            If GenerateurCode.EstCassable(ligne) Then
                n += 1
            End If
        Next
        Exiger(n > 0, "Aucune ligne cassable : le jeu serait impossible")
        _detail = n.ToString() & " lignes cassables sur 1000"
    End Sub

    Private Shared Sub T04_InsererErreur()
        Dim lignes As List(Of CodeLine) = GenerateurCode.GenererLignesValides(1000, New Random(7))
        Dim avant As New List(Of CodeLine)(lignes)

        Dim index As Integer = GenerateurCode.InsererErreur(lignes, New Random(11))
        Exiger(index >= 0 AndAlso index < lignes.Count, "Index d'erreur hors de la liste : " & index.ToString())
        Exiger(lignes(index).EstErreur, "La ligne choisie n'est pas marquee comme erreur")
        Exiger(CompterErreurs(lignes) = 1, "Il doit y avoir exactement une erreur")
        Exiger(lignes(index).NumeroLigne = index + 1, "Le numero de la ligne cassee a change")

        'La ligne cassee = la ligne d'origine sans son point-virgule final
        Dim attendu As String = avant(index).Contenu.TrimEnd()
        attendu = attendu.Substring(0, attendu.Length - 1)
        Exiger(lignes(index).Contenu = attendu, "La ligne cassee n'est pas l'originale sans point-virgule")

        'Toutes les autres lignes sont les memes objets qu'avant
        For i As Integer = 0 To lignes.Count - 1
            If i <> index Then
                Exiger(lignes(i) Is avant(i), "La ligne " & (i + 1).ToString() & " a ete modifiee")
            End If
        Next
    End Sub

    Private Shared Sub T05_DoubleErreur()
        Dim lignes As List(Of CodeLine) = GenerateurCode.GenererLignesValides(500, New Random(3))
        GenerateurCode.InsererErreur(lignes, New Random(5))
        Dim ex As Exception = LeveException(Sub() GenerateurCode.InsererErreur(lignes, New Random(6)))
        Exiger(TypeOf ex Is InvalidOperationException, "Une deuxieme erreur aurait du etre refusee")
    End Sub

    Private Shared Sub T06_LigneNonCassable()
        Dim lignes As List(Of CodeLine) = GenerateurCode.GenererLignesValides(1000, New Random(2))
        Dim trouvee As CodeLine = Nothing
        For Each ligne As CodeLine In lignes
            If Not GenerateurCode.EstCassable(ligne) Then
                trouvee = ligne
                Exit For
            End If
        Next
        Exiger(trouvee IsNot Nothing, "Aucune ligne non cassable trouvee pour le test")

        Dim ex As Exception = LeveException(Sub() GenerateurCode.CasserLigne(trouvee))
        Exiger(TypeOf ex Is InvalidOperationException, "CasserLigne aurait du refuser la ligne " & trouvee.NumeroLigne.ToString())
    End Sub

    Private Shared Sub T07_Aleatoire()
        Dim dossier As String = NouveauDossier()
        Try
            Dim jeu As New CodeJeu(dossier, 3000)
            Dim positions As New HashSet(Of Integer)
            For i As Integer = 1 To 60
                jeu.DemarrerPartie()
                positions.Add(jeu.IndexErreur)
                jeu.TerminerPartie()
            Next
            Exiger(positions.Count >= 10, "L'erreur tombe presque toujours au meme endroit (" & positions.Count.ToString() & " positions)")
            _detail = positions.Count.ToString() & " positions differentes sur 60 parties"
        Finally
            Supprimer(dossier)
        End Try
    End Sub

    Private Shared Sub T08_CycleCodeJeu()
        Dim dossier As String = NouveauDossier()
        Try
            Dim jeu As New CodeJeu(dossier, 2000)
            Exiger(Not jeu.PartieEnCours, "Une partie est en cours avant d'avoir demarre")
            Exiger(jeu.IndexErreur = -1, "IndexErreur devrait valoir -1 hors partie")
            Exiger(CompterErreurs(jeu.Courant) = 0, "Le programme de base contient une erreur")

            jeu.DemarrerPartie()
            Exiger(jeu.PartieEnCours, "La partie devrait etre en cours")
            Exiger(jeu.IndexErreur >= 0 AndAlso jeu.IndexErreur < jeu.NombreLignes, "IndexErreur hors limites")
            Exiger(jeu.LigneErreur IsNot Nothing AndAlso jeu.LigneErreur.EstErreur, "LigneErreur invalide")
            Exiger(CompterErreurs(jeu.Courant) = 1, "Il doit y avoir exactement une erreur pendant la partie")

            Dim ex As Exception = LeveException(Sub() jeu.DemarrerPartie())
            Exiger(TypeOf ex Is InvalidOperationException, "Demarrer une partie deja en cours devrait etre refuse")

            jeu.TerminerPartie()
            Exiger(Not jeu.PartieEnCours, "La partie devrait etre terminee")
            Exiger(jeu.IndexErreur = -1, "IndexErreur devrait revenir a -1")
            Exiger(CompterErreurs(jeu.Courant) = 0, "Le programme de base doit redevenir sans erreur")
        Finally
            Supprimer(dossier)
        End Try
    End Sub

    Private Shared Sub T09_Sauvegarde()
        Dim dossier As String = NouveauDossier()
        Try
            Dim premier As New CodeJeu(dossier, 1500)
            Dim second As New CodeJeu(dossier, 1500)
            Exiger(premier.NombreLignes = second.NombreLignes, "Le nombre de lignes a change apres rechargement")
            For i As Integer = 0 To premier.NombreLignes - 1
                Exiger(premier.Courant(i).Contenu = second.Courant(i).Contenu,
                       "La ligne " & (i + 1).ToString() & " est differente apres rechargement")
                Exiger(premier.Courant(i).NumeroLigne = second.Courant(i).NumeroLigne,
                       "Le numero de la ligne " & (i + 1).ToString() & " est different apres rechargement")
            Next
        Finally
            Supprimer(dossier)
        End Try
    End Sub

#End Region

#Region "Bureau, murs, plan"

    Private Shared Sub T10_Bureau()
        Dim lignes As List(Of CodeLine) = GenerateurCode.GenererLignesValides(1000, New Random(3))
        Dim bureau As New Bureau()
        bureau.RepartirLignes(lignes)

        Exiger(bureau.Murs.Count = 4, "Le bureau doit avoir 4 murs")
        Exiger(bureau.Murs(0).Orientation = Orientation.Nord, "Le premier mur doit etre le Nord")
        Exiger(bureau.Murs(1).Orientation = Orientation.Est, "Le deuxieme mur doit etre l'Est")
        Exiger(bureau.Murs(2).Orientation = Orientation.Sud, "Le troisieme mur doit etre le Sud")
        Exiger(bureau.Murs(3).Orientation = Orientation.Ouest, "Le quatrieme mur doit etre l'Ouest")
        Exiger(bureau.NombreLignes = 1000, "Il doit y avoir 1000 lignes en tout")

        For Each unMur As Mur In bureau.Murs
            Exiger(unMur.Lignes.Count > 0, "Un mur est vide")
        Next
        For i As Integer = 0 To 999
            Exiger(bureau.ObtenirLigne(i) Is lignes(i), "ObtenirLigne(" & i.ToString() & ") ne renvoie pas la bonne ligne")
        Next

        Dim ex1 As Exception = LeveException(Sub() bureau.ObtenirLigne(1000))
        Exiger(TypeOf ex1 Is ArgumentOutOfRangeException, "ObtenirLigne(1000) devrait etre refuse")
        Dim ex2 As Exception = LeveException(Sub() bureau.ObtenirLigne(-1))
        Exiger(TypeOf ex2 Is ArgumentOutOfRangeException, "ObtenirLigne(-1) devrait etre refuse")
    End Sub

    Private Shared Sub T11_MurVisible()
        Dim mur As New Mur(Orientation.Nord)
        For i As Integer = 1 To 10
            mur.AjouterLigne(New CodeLine(i, "x;"))
        Next

        Exiger(mur.ObtenirLigneVisible(0, 3).Count = 3, "(0, 3) devrait donner 3 lignes")
        Exiger(mur.ObtenirLigneVisible(8, 5).Count = 2, "(8, 5) devrait donner 2 lignes (fin du mur)")
        Exiger(mur.ObtenirLigneVisible(20, 5).Count = 0, "(20, 5) devrait donner 0 ligne")
        Exiger(mur.ObtenirLigneVisible(-5, 3).Count = 3, "(-5, 3) devrait commencer a la premiere ligne")
        Exiger(mur.ObtenirLigneVisible(0, 100).Count = 10, "(0, 100) devrait donner les 10 lignes")
        Exiger(mur.ObtenirLigneVisible(2, 3)(0).NumeroLigne = 3, "(2, 3) devrait commencer a la ligne numero 3")

        Dim ex As Exception = LeveException(Sub() mur.AjouterLigne(Nothing))
        Exiger(TypeOf ex Is ArgumentNullException, "Ajouter Nothing devrait etre refuse")
    End Sub

    Private Shared Sub T12_Carte()
        Dim plan As Carte = Carte.CreerParDefaut()
        Exiger(plan.TypeCase(-1, 0) = "#"c, "Une case a gauche du plan doit etre un mur")
        Exiger(plan.TypeCase(0, -1) = "#"c, "Une case au-dessus du plan doit etre un mur")
        Exiger(plan.TypeCase(plan.Largeur, 0) = "#"c, "Une case a droite du plan doit etre un mur")
        Exiger(plan.TypeCase(0, plan.Hauteur) = "#"c, "Une case sous le plan doit etre un mur")
        Exiger(plan.EstPlein(0, 0), "Le coin du plan doit etre plein")
        Exiger(Not plan.EstPlein(1, 1), "La premiere case du bureau doit etre libre")
        Exiger(plan.TypeCase(3, 3) = "C"c, "La case (3, 3) doit etre une cloison")
        Exiger(plan.EstPlein(3, 3), "Une cloison doit bloquer le passage")

        Dim ex As Exception = LeveException(AddressOf CreerCarteVide)
        Exiger(TypeOf ex Is ArgumentException, "Un plan vide devrait etre refuse")
    End Sub

#End Region

#Region "Joueur"

    Private Shared Sub T13_Inspecter()
        Dim joueur As New Joueur()
        Dim fautive As New CodeLine(1, "int x", True)
        Dim normale As New CodeLine(2, "int y;", False)

        Exiger(joueur.Inspecter(fautive), "La ligne fautive aurait du etre reconnue")
        Exiger(Not joueur.Inspecter(normale), "Une ligne normale ne doit pas etre reconnue comme fautive")
        Exiger(joueur.DerniereLigneInspectee Is normale, "La derniere ligne inspectee n'est pas memorisee")

        Dim ex As Exception = LeveException(Sub() joueur.Inspecter(Nothing))
        Exiger(TypeOf ex Is ArgumentNullException, "Inspecter(Nothing) devrait etre refuse")
    End Sub

    Private Shared Sub T14_Collisions()
        'Un petit bureau ferme avec une cloison au milieu
        Dim plan As New Carte(New String() {"#####", "#...#", "#.C.#", "#...#", "#####"})
        Dim joueur As New Joueur()
        joueur.Carte = plan

        'Vers l'est, le long du mur du fond
        joueur.PlacerEn(1.5F, 1.5F, 0.0F)
        For i As Integer = 1 To 300
            joueur.SeDeplacer(Direction.Haut)
            Exiger(Not plan.EstPlein(CInt(Math.Floor(joueur.PositionX)), CInt(Math.Floor(joueur.PositionY))),
                   "Le joueur est entre dans un mur (pas " & i.ToString() & ")")
        Next
        Exiger(joueur.PositionX < 4.0F, "Le joueur a traverse le mur de l'est")

        'Vers l'est, face a la cloison
        joueur.PlacerEn(1.5F, 2.5F, 0.0F)
        For i As Integer = 1 To 300
            joueur.SeDeplacer(Direction.Haut)
        Next
        Exiger(joueur.PositionX < 2.0F, "Le joueur a traverse la cloison")

        'En marche arriere, sans sortir du plan
        joueur.PlacerEn(1.5F, 1.5F, 0.0F)
        For i As Integer = 1 To 300
            joueur.SeDeplacer(Direction.Bas)
        Next
        Exiger(joueur.PositionX > 1.0F, "Le joueur a traverse le mur de l'ouest en reculant")

        'Un plan sans murs : le bord du plan arrete quand meme le joueur
        Dim ouvert As New Carte(New String() {"..", ".."})
        joueur.Carte = ouvert
        joueur.PlacerEn(0.9F, 0.9F, CSng(Math.PI))
        For i As Integer = 1 To 300
            joueur.SeDeplacer(Direction.Haut)
        Next
        Exiger(joueur.PositionX >= 0.0F, "Le joueur est sorti du plan par la gauche")
    End Sub

    Private Shared Sub T15_Rotation()
        Dim joueur As New Joueur()
        joueur.PlacerEn(0.0F, 0.0F, 0.0F)

        joueur.Tourner(1.0F)
        Exiger(Math.Abs(joueur.Angle - 1.0F) < 0.0001F, "Tourner(1) devrait donner un angle de 1")

        joueur.Tourner(10.0F)
        Exiger(joueur.Angle <= CSng(Math.PI) AndAlso joueur.Angle >= -CSng(Math.PI), "L'angle depasse Pi apres un grand virage")

        joueur.Tourner(-30.0F)
        Exiger(joueur.Angle <= CSng(Math.PI) AndAlso joueur.Angle >= -CSng(Math.PI), "L'angle depasse -Pi apres un grand virage")
    End Sub

#End Region

#Region "Moteur"

    Private Shared Sub T16_Moteur()
        Dim dossier As String = NouveauDossier()
        Dim moteur As MoteurJeu = Nothing
        Try
            Dim jeu As New CodeJeu(dossier, 2000)
            Dim bureau As New Bureau()
            Dim joueur As New Joueur()
            moteur = New MoteurJeu(jeu, bureau, joueur)

            Dim nbVictoires As Integer = 0
            Dim nbVerifications As Integer = 0
            AddHandler moteur.PartieTerminee, Sub(t As TimeSpan, n As Integer) nbVictoires += 1
            AddHandler moteur.LigneVerifiee, Sub(l As CodeLine, ok As Boolean) nbVerifications += 1

            moteur.DemarrerPartie()
            Exiger(moteur.PartieEnCours, "La partie devrait etre en cours")
            Exiger(moteur.Chronometre.IsRunning, "Le chrono devrait tourner")
            Exiger(moteur.NbTentatives = 0, "Le compteur d'essais doit partir de zero")
            Exiger(bureau.NombreLignes = 2000, "Les lignes n'ont pas ete reparties sur les murs")

            'Deux mauvaises lignes
            Dim fausse1 As CodeLine = LigneSansErreur(bureau, 0)
            Dim fausse2 As CodeLine = LigneSansErreur(bureau, fausse1.NumeroLigne + 10)
            Exiger(Not moteur.VerifierVictoire(fausse1), "Une mauvaise ligne ne doit pas faire gagner")
            Exiger(moteur.NbTentatives = 1, "Le premier essai n'est pas compte")
            Exiger(Not moteur.VerifierVictoire(fausse2), "Une mauvaise ligne ne doit pas faire gagner")
            Exiger(moteur.NbTentatives = 2, "Le deuxieme essai n'est pas compte")
            Exiger(moteur.PartieEnCours, "La partie ne doit pas s'arreter sur une mauvaise ligne")
            Exiger(nbVictoires = 0, "Evenement PartieTerminee declenche trop tot")

            'La bonne ligne
            Exiger(moteur.VerifierVictoire(jeu.LigneErreur), "La ligne fautive aurait du faire gagner")
            Exiger(moteur.NbTentatives = 3, "Le troisieme essai n'est pas compte")
            Exiger(nbVictoires = 1, "PartieTerminee doit etre declenche une seule fois")
            Exiger(Not moteur.Chronometre.IsRunning, "Le chrono doit s'arreter a la victoire")
            Exiger(Not moteur.PartieEnCours, "La partie doit etre terminee")
            Exiger(moteur.NbLignesDistinctes = 3, "3 lignes differentes ont ete inspectees")

            'Apres la victoire, plus rien n'est compte
            Exiger(Not moteur.VerifierVictoire(fausse1), "Apres la victoire, aucune ligne ne doit etre acceptee")
            Exiger(moteur.NbTentatives = 3, "Les essais ne doivent plus augmenter apres la victoire")
            Exiger(nbVerifications = 3, "Evenement LigneVerifiee : 3 attendus")

            Dim ex As Exception = LeveException(Sub() moteur.VerifierVictoire(Nothing))
            Exiger(TypeOf ex Is ArgumentNullException, "VerifierVictoire(Nothing) devrait etre refuse")

            'Une nouvelle partie remet tout a zero
            moteur.DemarrerPartie()
            Exiger(moteur.NbTentatives = 0, "Une nouvelle partie doit remettre les essais a zero")
            Exiger(moteur.Chronometre.IsRunning, "Une nouvelle partie doit relancer le chrono")
        Finally
            If moteur IsNot Nothing Then
                moteur.Dispose()
            End If
            Supprimer(dossier)
        End Try
    End Sub

#End Region

#Region "Performance"

    Private Shared Sub T17_Performance()
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Dim lignes As List(Of CodeLine) = GenerateurCode.GenererLignesValides(GenerateurCode.NombreLignesParDefaut)
        Dim tGeneration As Long = chrono.ElapsedMilliseconds
        Exiger(lignes.Count = GenerateurCode.NombreLignesParDefaut, "Mauvais nombre de lignes generees")

        chrono.Restart()
        Dim bureau As New Bureau()
        bureau.RepartirLignes(lignes)
        Dim tRepartition As Long = chrono.ElapsedMilliseconds

        chrono.Restart()
        Dim alea As New Random(5)
        For i As Integer = 1 To 20000
            bureau.ObtenirLigne(alea.Next(lignes.Count))
        Next
        Dim tLecture As Long = chrono.ElapsedMilliseconds

        _detail = "generation " & tGeneration.ToString() & " ms  |  repartition " & tRepartition.ToString() &
                  " ms  |  20 000 lectures " & tLecture.ToString() & " ms"

        Exiger(tGeneration < 30000, "La generation est trop lente (plus de 30 s)")
        Exiger(tRepartition < 2000, "La repartition sur les murs est trop lente (plus de 2 s)")
        Exiger(tLecture < 1000, "La lecture de 20 000 lignes est trop lente (plus de 1 s)")
    End Sub

#End Region

End Class
