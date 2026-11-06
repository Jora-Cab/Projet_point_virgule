Option Explicit On
Option Strict On

Imports System.Collections.Generic

''' <summary>
''' Classe utilitaire : génère un programme C crédible (conventions du C) et fournit les outils
''' pour "casser" une ligne (retirer son point-virgule).
''' Convention : NumeroLigne est base 1, donc index dans la liste = NumeroLigne - 1.
''' </summary>
Public NotInheritable Class GenerateurCode

    Public Const NombreLignesParDefaut As Integer = 187421

    ''' <summary>Niveau d'indentation maximal (1 = corps d'une fonction).</summary>
    Private Const ProfondeurMax As Integer = 4

    ''' <summary>Nombre de tirages aléatoires avant de passer au balayage complet (cas pathologique).</summary>
    Private Const EssaisAleatoires As Integer = 2000

    Private Const Guillemet As String = """"

    Private Sub New()
        ' Classe utilitaire : pas d'instanciation.
    End Sub

#Region "Vocabulaire C"

    Private Shared ReadOnly RacinesModule As String() = {
        "net", "buffer", "parser", "cache", "queue", "list", "timer", "logger", "config", "session",
        "packet", "hash", "stream", "sched", "alloc", "token", "codec", "table", "index", "event"}
    Private Shared ReadOnly SuffixesModule As String() = {"core", "io", "util", "impl", "ops", "state"}
    Private Shared ReadOnly Verbes As String() = {
        "init", "open", "close", "read", "write", "parse", "check", "update", "flush", "reset",
        "alloc", "release", "find", "insert", "remove", "compute", "encode", "decode", "copy",
        "compare", "scan", "load", "store", "send", "recv"}
    Private Shared ReadOnly Noms As String() = {
        "buffer", "node", "entry", "packet", "header", "frame", "record", "block", "chunk",
        "item", "token", "state", "table", "index", "queue"}
    Private Shared ReadOnly TypesRetour As String() = {
        "int", "int", "int", "void", "void", "size_t", "unsigned int", "char *", "double", "long"}
    Private Shared ReadOnly ExtrasInclude As String() = {
        "stdint.h", "stddef.h", "errno.h", "ctype.h", "time.h", "assert.h", "stdarg.h", "math.h"}
    Private Shared ReadOnly MacrosExtra As String() = {
        "TIMEOUT_MS 500", "MAX_RETRIES 3", "FLAG_ACTIVE 0x01", "FLAG_DIRTY 0x02", "VERSION 2", "HEADER_SIZE 16"}
    Private Shared ReadOnly TaillesTampon As String() = {"64", "128", "256", "512", "1024", "4096"}
    Private Shared ReadOnly TaillesMax As String() = {"16", "32", "64", "100", "128", "256"}
    Private Shared ReadOnly Etats As String() = {
        "IDLE", "OPEN", "BUSY", "CLOSED", "ERROR", "READY", "WAITING", "DONE"}
    Private Shared ReadOnly ChampsPossibles As String() = {
        "id", "flags", "count", "length", "offset", "size", "status"}
    Private Shared ReadOnly NomsTable As String() = {"alpha", "beta", "gamma", "delta", "omega"}
    Private Shared ReadOnly DescriptionsModule As String() = {
        "core routines", "buffer handling", "low-level helpers", "state machine",
        "input and output helpers", "internal data structures"}
    Private Shared ReadOnly DescriptionsFonction As String() = {
        "initialize the internal state", "validate the given arguments", "process the next pending item",
        "release all allocated resources", "update the counters", "look up an entry by key",
        "copy the data into the buffer", "flush pending operations"}
    Private Shared ReadOnly Commentaires As String() = {
        "check bounds", "update state", "handle the special case", "main processing loop",
        "release resources", "retry on failure", "skip empty entries", "reset the counters"}
    Private Shared ReadOnly MotsAffichage As String() = {
        "value", "count", "status", "offset", "result", "size"}
    Private Shared ReadOnly MessagesErreur As String() = {
        "error: out of memory", "error: invalid argument", "warning: retrying", "error: read failed"}
    Private Shared ReadOnly Compteurs As String() = {"i", "j"}

    ' Mots-clés C dont la ligne ne doit JAMAIS être choisie comme erreur.
    Private Shared ReadOnly MotsCles As String() = {
        "if", "for", "while", "else", "switch", "do", "case", "default"}

    ' Indentations pré-calculées (4 espaces par niveau) : évite une chaîne par ligne générée.
    Private Shared ReadOnly Indentations As String() = CreerIndentations(8)

#End Region

#Region "Types internes"

    Private Enum GenreParam
        Entier = 0
        Reel = 1
        Chaine = 2
        Enregistrement = 3
    End Enum

    Private NotInheritable Class ParamC
        Public ReadOnly Declaration As String
        Public ReadOnly Genre As GenreParam
        Public ReadOnly Nom As String

        Public Sub New(declaration As String, genre As GenreParam, nom As String)
            Me.Declaration = declaration
            Me.Genre = genre
            Me.Nom = nom
        End Sub
    End Class

    Private NotInheritable Class FonctionC
        Public ReadOnly Nom As String
        Public ReadOnly Retour As String
        Public ReadOnly EstStatique As Boolean
        Public ReadOnly Parametres As List(Of ParamC)

        Public Sub New(nom As String, retour As String, estStatique As Boolean, parametres As List(Of ParamC))
            Me.Nom = nom
            Me.Retour = retour
            Me.EstStatique = estStatique
            Me.Parametres = parametres
        End Sub

        Public Function Signature() As String
            Dim liste As String
            If Parametres.Count = 0 Then
                liste = "void"
            Else
                Dim declarations As New List(Of String)()
                For Each p As ParamC In Parametres
                    declarations.Add(p.Declaration)
                Next
                liste = String.Join(", ", declarations)
            End If
            Dim prefixe As String = If(EstStatique, "static ", "")
            Dim separateur As String = If(Retour.EndsWith("*", StringComparison.Ordinal), "", " ")
            Return prefixe & Retour & separateur & Nom & "(" & liste & ")"
        End Function

        Public Function RetourneUnEntier() As Boolean
            Return Retour = "int" OrElse Retour = "long" OrElse Retour = "size_t" OrElse Retour = "unsigned int"
        End Function
    End Class

    Private NotInheritable Class ModuleC
        Public ReadOnly Prefixe As String
        Public ReadOnly PrefixeMajuscule As String
        Public ReadOnly NomType As String
        Public ReadOnly ChampsEntiers As New List(Of String)()
        Public ReadOnly Fonctions As New List(Of FonctionC)()

        Public Sub New(prefixe As String)
            Me.Prefixe = prefixe
            Me.PrefixeMajuscule = prefixe.ToUpperInvariant()
            Me.NomType = prefixe & "_item_t"
        End Sub
    End Class

    Private NotInheritable Class ContexteFonction
        Public ReadOnly Unite As ModuleC
        Public ReadOnly Fonction As FonctionC
        Public ReadOnly Entiers As New List(Of String)()
        Public ReadOnly Reels As New List(Of String)()
        Public ReadOnly Chaines As New List(Of String)()
        Public ReadOnly Pointeurs As New List(Of String)()
        Public ReadOnly Tas As New List(Of String)()
        Public ReadOnly Tampons As New List(Of String)()
        Public ReadOnly Enregistrements As New List(Of String)()
        Public ProfondeurBoucle As Integer

        Public Sub New(unite As ModuleC, fonction As FonctionC)
            Me.Unite = unite
            Me.Fonction = fonction
        End Sub
    End Class

#End Region

#Region "API publique"
    ''' <summary>Génère 'nombre' lignes de code C valides (aucune erreur).</summary>
    Public Shared Function GenererLignesValides(nombre As Integer) As List(Of CodeLine)
        Return GenererLignesValides(nombre, NouveauRandom())
    End Function

    ''' <summary>Surcharge avec Random fourni (graine fixe = génération reproductible pour les tests).</summary>
    Public Shared Function GenererLignesValides(nombre As Integer, rnd As Random) As List(Of CodeLine)
        If nombre <= 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(nombre), "Le nombre de lignes doit être positif.")
        End If
        If rnd Is Nothing Then
            Throw New ArgumentNullException(NameOf(rnd))
        End If

        Dim textes As New List(Of String)(nombre + 256)
        Do While textes.Count < nombre
            AjouterModule(textes, nombre, rnd)
        Loop

        Dim lignes As New List(Of CodeLine)(nombre)
        For i As Integer = 0 To nombre - 1
            lignes.Add(New CodeLine(i + 1, textes(i), False))
        Next
        Return lignes

    End Function
    ''' <summary> Nouveau générateur aléatoire dont la graine est toujours différente, même appelé très vite.</summary>
    Public Shared Function NouveauRandom() As Random
        Return New Random(Guid.NewGuid().GetHashCode())
    End Function

    ''' <summary>
    ''' True si la ligne se termine par un point-virgule ET que sa suppression crée une vraie erreur C.
    ''' Exclut : if/for/while/else/switch/do/case/default, lignes #, commentaires, lignes vides.
    ''' N'alloue aucune chaîne.
    ''' </summary>
    Public Shared Function EstCassable(ligne As CodeLine) As Boolean
        If ligne Is Nothing OrElse ligne.EstErreur Then
            Return False
        End If

        Dim s As String = ligne.Contenu
        If s Is Nothing Then
            Return False
        End If

        ' Dernier caractère significatif : doit être ';'
        Dim fin As Integer = s.Length - 1
        Do While fin >= 0 AndAlso Char.IsWhiteSpace(s(fin))
            fin -= 1
        Loop
        If fin < 1 OrElse s(fin) <> ";"c Then
            Return False
        End If

        ' Premier caractère significatif
        Dim debut As Integer = 0
        Do While debut < fin AndAlso Char.IsWhiteSpace(s(debut))
            debut += 1
        Loop
        If debut >= fin Then
            Return False    ' ligne réduite à un simple ";"
        End If

        ' Directives du préprocesseur et commentaires : jamais cassables.
        Select Case s(debut)
            Case "#"c
                Return False
            Case "/"c
                If s(debut + 1) = "/"c OrElse s(debut + 1) = "*"c Then
                    Return False
                End If
            Case "*"c
                If s(debut + 1) = " "c OrElse s(debut + 1) = "/"c Then
                    Return False
                End If
        End Select

        ' On ignore les "}" et espaces initiaux : "} while (x);" est traité comme une ligne while.
        Do While debut < fin AndAlso (s(debut) = "}"c OrElse Char.IsWhiteSpace(s(debut)))
            debut += 1
        Loop

        Return Not CommenceParMotCle(s, debut, fin)
    End Function

    ''' <summary>Retourne une NOUVELLE ligne identique mais sans son point-virgule final, marquée EstErreur.</summary>
    Public Shared Function CasserLigne(ligne As CodeLine) As CodeLine
        If ligne Is Nothing Then
            Throw New ArgumentNullException(NameOf(ligne))
        End If
        If Not EstCassable(ligne) Then
            Throw New InvalidOperationException(
                "La ligne " & ligne.NumeroLigne.ToString() & " ne peut pas être choisie comme erreur.")
        End If

        Dim texte As String = ligne.Contenu.TrimEnd()
        Dim casse As String = texte.Substring(0, texte.Length - 1)

        Return New CodeLine(ligne.NumeroLigne, casse, True) With {
            .PositionX = ligne.PositionX,
            .PositionY = ligne.PositionY
        }
    End Function

    Public Shared Function InsererErreur(lignes As List(Of CodeLine)) As Integer
        Return InsererErreur(lignes, NouveauRandom())
    End Function
    ''' <summary>
    ''' Choisit au hasard (uniformément) une ligne cassable, la remplace par sa version cassee 
    ''' DANS CETTE LISTE et retourne son index (0-based). Les CodeLine d'origine ne sont jamais modifiés.
    ''' Méthode : tirage d'un index puis test (en moyenne 1 à 2 essais) ; un balayage complet
    ''' ne sert que de secours si les lignes cassables sont très rares.
    ''' </summary>
    Public Shared Function InsererErreur(lignes As List(Of CodeLine), rnd As Random) As Integer
        If lignes Is Nothing Then
            Throw New ArgumentNullException(NameOf(lignes))
        End If
        If rnd Is Nothing Then
            Throw New ArgumentNullException(NameOf(rnd))
        End If
        If lignes.Count = 0 Then
            Throw New InvalidOperationException("Aucune ligne ne peut être cassée.")
        End If

        For i As Integer = 0 To lignes.Count - 1
            If lignes(i) IsNot Nothing AndAlso lignes(i).EstErreur Then
                Throw New InvalidOperationException("La liste contient déjà une ligne erronée.")
            End If
        Next

        Dim index As Integer = -1
        For essai As Integer = 1 To EssaisAleatoires
            Dim candidat As Integer = rnd.Next(lignes.Count)
            If EstCassable(lignes(candidat)) Then
                index = candidat
                Exit For
            End If
        Next

        If index < 0 Then
            index = ChoisirParBalayage(lignes, rnd)
        End If

        lignes(index) = CasserLigne(lignes(index))
        Return index
    End Function

#End Region

#Region "Utilitaires"

    Private Shared Function ChoisirParBalayage(lignes As List(Of CodeLine), rnd As Random) As Integer
        Dim candidats As New List(Of Integer)()
        For i As Integer = 0 To lignes.Count - 1
            If EstCassable(lignes(i)) Then
                candidats.Add(i)
            End If
        Next

        If candidats.Count = 0 Then
            Throw New InvalidOperationException("Aucune ligne ne peut être cassée.")
        End If
        Return candidats(rnd.Next(candidats.Count))
    End Function

    ''' <summary>True si s(debut..fin) commence par un mot-clé de MotsCles (mot entier).</summary>
    Private Shared Function CommenceParMotCle(s As String, debut As Integer, fin As Integer) As Boolean
        For Each motCle As String In MotsCles
            Dim longueur As Integer = motCle.Length
            If debut + longueur <= fin AndAlso String.CompareOrdinal(s, debut, motCle, 0, longueur) = 0 Then
                Dim suivant As Char = s(debut + longueur)
                If Not (Char.IsLetterOrDigit(suivant) OrElse suivant = "_"c) Then
                    Return True
                End If
            End If
        Next
        Return False
    End Function

    Private Shared Function CreerIndentations(niveaux As Integer) As String()
        Dim resultat(niveaux - 1) As String
        For i As Integer = 0 To niveaux - 1
            resultat(i) = New String(" "c, i * 4)
        Next
        Return resultat
    End Function

    Private Shared Function Choisir(Of T)(valeurs As T(), rnd As Random) As T
        Return valeurs(rnd.Next(valeurs.Length))
    End Function

    Private Shared Function ChoisirDans(Of T)(valeurs As List(Of T), rnd As Random) As T
        Return valeurs(rnd.Next(valeurs.Count))
    End Function

    Private Shared Function Indent(niveau As Integer) As String
        If niveau < Indentations.Length Then
            Return Indentations(niveau)
        End If
        Return New String(" "c, niveau * 4)
    End Function

#End Region

#Region "Génération d'un module .c"

    Private Shared Sub AjouterModule(res As List(Of String), nombre As Integer, rnd As Random)
        Dim m As New ModuleC(Choisir(RacinesModule, rnd))
        Dim nomFichier As String = m.Prefixe & "_" & Choisir(SuffixesModule, rnd) & ".c"

        AjouterEnteteModule(res, m, nomFichier, rnd)

        ' Les fonctions sont décidées d'abord pour pouvoir écrire les prototypes des fonctions static.
        Dim aDefinir As New List(Of FonctionC)()
        For k As Integer = 1 To rnd.Next(5, 13)
            aDefinir.Add(CreerFonction(m, aDefinir, rnd))
        Next

        Dim prototypes As Integer = 0
        For Each f As FonctionC In aDefinir
            If f.EstStatique Then
                res.Add(f.Signature() & ";")
                prototypes += 1
            End If
        Next
        If prototypes > 0 Then
            res.Add("")
        End If

        For Each f As FonctionC In aDefinir
            If res.Count >= nombre Then
                Exit For
            End If
            AjouterFonction(res, f, m, rnd)
            m.Fonctions.Add(f)
        Next
    End Sub

    Private Shared Sub AjouterEnteteModule(res As List(Of String), m As ModuleC,
                                           nomFichier As String, rnd As Random)
        Dim p As String = m.PrefixeMajuscule

        res.Add("/*")
        res.Add(" * " & nomFichier & " - " & Choisir(DescriptionsModule, rnd) & ".")
        res.Add(" *")
        res.Add(" * Copyright (c) 2026 The Project Authors. All rights reserved.")
        res.Add(" */")
        res.Add("")

        ' Convention : en-têtes système d'abord, puis en-têtes locaux.
        res.Add("#include <stdio.h>")
        res.Add("#include <stdlib.h>")
        res.Add("#include <string.h>")
        Dim debutInclude As Integer = rnd.Next(ExtrasInclude.Length)
        For k As Integer = 0 To rnd.Next(0, 4) - 1
            res.Add("#include <" & ExtrasInclude((debutInclude + k) Mod ExtrasInclude.Length) & ">")
        Next
        res.Add("")
        res.Add("#include " & Guillemet & m.Prefixe & ".h" & Guillemet)
        res.Add("#include " & Guillemet & "common.h" & Guillemet)
        res.Add("")

        res.Add("#define " & p & "_BUFFER_SIZE " & Choisir(TaillesTampon, rnd))
        res.Add("#define " & p & "_MAX_ITEMS " & Choisir(TaillesMax, rnd))
        Dim debutMacro As Integer = rnd.Next(MacrosExtra.Length)
        For k As Integer = 0 To rnd.Next(0, 3) - 1
            res.Add("#define " & p & "_" & MacrosExtra((debutMacro + k) Mod MacrosExtra.Length))
        Next
        res.Add("")

        ' enum
        res.Add("enum " & m.Prefixe & "_state {")
        Dim nbEtats As Integer = rnd.Next(3, 6)
        Dim debutEtat As Integer = rnd.Next(Etats.Length)
        For k As Integer = 0 To nbEtats - 1
            Dim virgule As String = If(k < nbEtats - 1, ",", "")
            res.Add(Indent(1) & p & "_STATE_" & Etats((debutEtat + k) Mod Etats.Length) & virgule)
        Next
        res.Add("};")
        res.Add("")

        ' struct
        res.Add("typedef struct " & m.Prefixe & "_item {")
        Dim debutChamp As Integer = rnd.Next(ChampsPossibles.Length)
        For k As Integer = 0 To rnd.Next(2, 5) - 1
            Dim champ As String = ChampsPossibles((debutChamp + k) Mod ChampsPossibles.Length)
            res.Add(Indent(1) & TypeChamp(champ) & " " & champ & ";")
            m.ChampsEntiers.Add(champ)
        Next
        res.Add(Indent(1) & "char *name;")
        res.Add(Indent(1) & "struct " & m.Prefixe & "_item *next;")
        res.Add("} " & m.NomType & ";")
        res.Add("")

        ' variables globales static
        res.Add("static int " & m.Prefixe & "_count = 0;")
        If rnd.Next(2) = 0 Then
            res.Add("static unsigned int " & m.Prefixe & "_flags = 0;")
        End If
        If rnd.Next(2) = 0 Then
            res.Add("static const char *" & m.Prefixe & "_names[] = {")
            For k As Integer = 0 To 2
                res.Add(Indent(1) & Guillemet & NomsTable(k) & Guillemet & ",")
            Next
            res.Add("};")
        End If
        res.Add("")
    End Sub

    Private Shared Function TypeChamp(champ As String) As String
        Select Case champ
            Case "length", "size"
                Return "size_t"
            Case "flags"
                Return "unsigned int"
            Case Else
                Return "int"
        End Select
    End Function

#End Region

#Region "Génération d'une fonction"

    Private Shared Function NomPris(existantes As List(Of FonctionC), nom As String) As Boolean
        For Each f As FonctionC In existantes
            If String.Equals(f.Nom, nom, StringComparison.Ordinal) Then
                Return True
            End If
        Next
        Return False
    End Function

    Private Shared Function CreerParam(m As ModuleC, genre As Integer) As ParamC
        Select Case genre
            Case 0
                Return New ParamC("int id", GenreParam.Entier, "id")
            Case 1
                Return New ParamC("size_t len", GenreParam.Entier, "len")
            Case 2
                Return New ParamC("unsigned int flags", GenreParam.Entier, "flags")
            Case 3
                Return New ParamC("const char *name", GenreParam.Chaine, "name")
            Case 4
                Return New ParamC("double ratio", GenreParam.Reel, "ratio")
            Case Else
                Return New ParamC(m.NomType & " *item", GenreParam.Enregistrement, "item")
        End Select
    End Function

    Private Shared Function CreerFonction(m As ModuleC, existantes As List(Of FonctionC),
                                           rnd As Random) As FonctionC
        Dim racine As String = m.Prefixe & "_" & Choisir(Verbes, rnd) & "_" & Choisir(Noms, rnd)
        Dim nom As String = racine
        Dim suffixe As Integer = 2
        Do While NomPris(existantes, nom)
            nom = racine & "_" & suffixe.ToString()
            suffixe += 1
        Loop

        Dim parametres As New List(Of ParamC)()
        Dim debut As Integer = rnd.Next(6)
        For k As Integer = 0 To rnd.Next(0, 4) - 1
            parametres.Add(CreerParam(m, (debut + k) Mod 6))
        Next

        Return New FonctionC(nom, Choisir(TypesRetour, rnd), rnd.Next(2) = 0, parametres)
    End Function

    Private Shared Sub AjouterFonction(res As List(Of String), f As FonctionC, m As ModuleC, rnd As Random)
        Dim ctx As New ContexteFonction(m, f)

        For Each p As ParamC In f.Parametres
            Select Case p.Genre
                Case GenreParam.Entier
                    ctx.Entiers.Add(p.Nom)
                Case GenreParam.Reel
                    ctx.Reels.Add(p.Nom)
                Case GenreParam.Chaine
                    ctx.Chaines.Add(p.Nom)
                    ctx.Pointeurs.Add(p.Nom)
                Case Else
                    ctx.Enregistrements.Add(p.Nom)
                    ctx.Pointeurs.Add(p.Nom)
            End Select
        Next

        ' Convention C89 : toutes les variables locales déclarées en début de fonction.
        Dim declarations As New List(Of String)()
        declarations.Add("int i, j;")

        If f.Retour <> "void" Then
            Select Case f.Retour
                Case "double"
                    declarations.Add("double ret = 0.0;")
                    ctx.Reels.Add("ret")
                Case "char *"
                    declarations.Add("char *ret = NULL;")
                    ctx.Pointeurs.Add("ret")
                Case Else
                    declarations.Add(f.Retour & " ret = 0;")
                    ctx.Entiers.Add("ret")
            End Select
        End If

        declarations.Add("int status = 0;")
        ctx.Entiers.Add("status")

        If rnd.Next(2) = 0 Then
            declarations.Add("size_t n = 0;")
            ctx.Entiers.Add("n")
        End If
        If rnd.Next(3) = 0 Then
            declarations.Add("unsigned int mask = 0;")
            ctx.Entiers.Add("mask")
        End If
        If rnd.Next(3) = 0 Then
            declarations.Add("double sum = 0.0;")
            ctx.Reels.Add("sum")
        End If
        If rnd.Next(3) = 0 Then
            declarations.Add("char *p = NULL;")
            ctx.Tas.Add("p")
            ctx.Pointeurs.Add("p")
        End If
        If rnd.Next(3) = 0 Then
            declarations.Add("char buf[" & m.PrefixeMajuscule & "_BUFFER_SIZE];")
            ctx.Tampons.Add("buf")
        End If

        res.Add("/*")
        res.Add(" * " & f.Nom & " - " & Choisir(DescriptionsFonction, rnd) & ".")
        res.Add(" */")
        res.Add(f.Signature())
        res.Add("{")
        For Each d As String In declarations
            res.Add(Indent(1) & d)
        Next
        res.Add("")

        AjouterInstructions(res, ctx, 1, rnd.Next(4, 11), rnd)

        If f.Retour <> "void" Then
            res.Add("")
            res.Add(Indent(1) & "return ret;")
        End If
        res.Add("}")
        res.Add("")
    End Sub

#End Region

#Region "Instructions, blocs, expressions"

    Private Shared Sub AjouterInstructions(res As List(Of String), ctx As ContexteFonction,
                                           niveau As Integer, nombre As Integer, rnd As Random)
        For k As Integer = 1 To nombre
            Dim tirage As Integer = rnd.Next(0, 100)
            ' Première et dernière instruction d'un bloc : toujours une instruction simple.
            If k = 1 OrElse k = nombre OrElse niveau >= ProfondeurMax OrElse tirage < 62 Then
                res.Add(Indent(niveau) & Instruction(ctx, rnd))
            ElseIf tirage < 68 Then
                res.Add("")
            ElseIf tirage < 74 Then
                res.Add(Indent(niveau) & "/* " & Choisir(Commentaires, rnd) & " */")
            Else
                AjouterBloc(res, ctx, niveau, rnd)
            End If
        Next
    End Sub

    Private Shared Sub AjouterBloc(res As List(Of String), ctx As ContexteFonction,
                                   niveau As Integer, rnd As Random)
        Dim marge As String = Indent(niveau)
        Dim genreBloc As Integer = rnd.Next(0, 7)
        If (genreBloc = 3 OrElse genreBloc = 4) AndAlso ctx.ProfondeurBoucle >= 2 Then
            genreBloc = 0
        End If

        Select Case genreBloc
            Case 0, 1, 2
                res.Add(marge & "if (" & Condition(ctx, rnd) & ") {")
                AjouterInstructions(res, ctx, niveau + 1, rnd.Next(1, 5), rnd)
                If rnd.Next(0, 4) = 0 Then
                    res.Add(Indent(niveau + 1) & RetourAnticipe(ctx))
                End If
                If rnd.Next(0, 3) = 0 Then
                    res.Add(marge & "} else {")
                    AjouterInstructions(res, ctx, niveau + 1, rnd.Next(1, 4), rnd)
                End If
                res.Add(marge & "}")

            Case 3, 4
                Dim compteur As String = Compteurs(ctx.ProfondeurBoucle)
                res.Add(marge & "for (" & compteur & " = 0; " & compteur & " < " & Limite(ctx, rnd) &
                        "; " & compteur & "++) {")
                ctx.ProfondeurBoucle += 1
                AjouterInstructions(res, ctx, niveau + 1, rnd.Next(2, 6), rnd)
                ctx.ProfondeurBoucle -= 1
                res.Add(marge & "}")

            Case 5
                res.Add(marge & "while (" & Condition(ctx, rnd) & ") {")
                res.Add(Indent(niveau + 1) & "status++;")
                AjouterInstructions(res, ctx, niveau + 1, rnd.Next(1, 4), rnd)
                res.Add(marge & "}")

            Case Else
                ' Convention (style noyau Linux) : "case" aligné sur "switch".
                res.Add(marge & "switch (" & ChoisirDans(ctx.Entiers, rnd) & ") {")
                For c As Integer = 0 To rnd.Next(2, 4) - 1
                    res.Add(marge & "case " & c.ToString() & ":")
                    AjouterInstructions(res, ctx, niveau + 1, rnd.Next(1, 3), rnd)
                    res.Add(Indent(niveau + 1) & "break;")
                Next
                res.Add(marge & "default:")
                res.Add(Indent(niveau + 1) & "break;")
                res.Add(marge & "}")
        End Select
    End Sub

    Private Shared Function RetourAnticipe(ctx As ContexteFonction) As String
        Select Case ctx.Fonction.Retour
            Case "void"
                Return "return;"
            Case "char *"
                Return "return NULL;"
            Case "double"
                Return "return 0.0;"
            Case "size_t", "unsigned int"
                Return "return 0;"
            Case Else
                Return "return -1;"
        End Select
    End Function

    Private Shared Function Limite(ctx As ContexteFonction, rnd As Random) As String
        Select Case rnd.Next(0, 4)
            Case 0
                Return ctx.Unite.PrefixeMajuscule & "_MAX_ITEMS"
            Case 1
                Return rnd.Next(4, 65).ToString()
            Case Else
                Return ChoisirDans(ctx.Entiers, rnd)
        End Select
    End Function

    ''' <summary>Une instruction simple : elle se termine TOUJOURS par un point-virgule.</summary>
    Private Shared Function Instruction(ctx As ContexteFonction, rnd As Random) As String
        Select Case rnd.Next(0, 16)
            Case 0, 1, 2
                Return ChoisirDans(ctx.Entiers, rnd) & " = " & Expression(ctx, rnd) & ";"
            Case 3
                Return ChoisirDans(ctx.Entiers, rnd) & " += " & Operande(ctx, rnd) & ";"
            Case 4
                Return ChoisirDans(ctx.Entiers, rnd) & "++;"
            Case 5, 6
                Return AppelInterne(ctx, rnd)
            Case 7
                Return String.Format("printf(""{0}: %d\n"", {1});",
                                     Choisir(MotsAffichage, rnd), ChoisirDans(ctx.Entiers, rnd))
            Case 8
                Return String.Format("fprintf(stderr, ""{0}\n"");", Choisir(MessagesErreur, rnd))
            Case 9
                If ctx.Tampons.Count > 0 Then
                    If rnd.Next(2) = 0 Then
                        Return String.Format("memset({0}, 0, sizeof({0}));", ctx.Tampons(0))
                    End If
                    Return String.Format("snprintf({0}, sizeof({0}), ""%d"", {1});",
                                         ctx.Tampons(0), ChoisirDans(ctx.Entiers, rnd))
                End If
                Return ChoisirDans(ctx.Entiers, rnd) & "--;"
            Case 10
                If ctx.Tas.Count > 0 Then
                    Dim p As String = ctx.Tas(0)
                    Select Case rnd.Next(0, 3)
                        Case 0
                            Return p & " = (char *)malloc(" & ctx.Unite.PrefixeMajuscule & "_BUFFER_SIZE);"
                        Case 1
                            Return "free(" & p & ");"
                        Case Else
                            Return p & " = NULL;"
                    End Select
                End If
                Return ChoisirDans(ctx.Entiers, rnd) & " = 0;"
            Case 11
                If ctx.Chaines.Count > 0 Then
                    Return ChoisirDans(ctx.Entiers, rnd) & " = (int)strlen(" & ctx.Chaines(0) & ");"
                End If
                Return ChoisirDans(ctx.Entiers, rnd) & " -= " & Operande(ctx, rnd) & ";"
            Case 12
                If ctx.Reels.Count > 0 Then
                    Return ChoisirDans(ctx.Reels, rnd) & " += (double)" & ChoisirDans(ctx.Entiers, rnd) & " * 0.5;"
                End If
                Return ChoisirDans(ctx.Entiers, rnd) & " = " & Operande(ctx, rnd) & ";"
            Case 13
                If ctx.Enregistrements.Count > 0 Then
                    Return ctx.Enregistrements(0) & "->" & ChoisirDans(ctx.Unite.ChampsEntiers, rnd) &
                           " = " & Expression(ctx, rnd) & ";"
                End If
                Return ChoisirDans(ctx.Entiers, rnd) & " = " & Operande(ctx, rnd) & ";"
            Case 14
                Return ctx.Unite.Prefixe & "_count++;"
            Case Else
                Return ChoisirDans(ctx.Entiers, rnd) & " = " & Operande(ctx, rnd) & ";"
        End Select
    End Function

    Private Shared Function AppelInterne(ctx As ContexteFonction, rnd As Random) As String
        If ctx.Unite.Fonctions.Count = 0 Then
            Return ChoisirDans(ctx.Entiers, rnd) & " = " & Operande(ctx, rnd) & ";"
        End If

        Dim cible As FonctionC = ChoisirDans(ctx.Unite.Fonctions, rnd)
        Dim arguments As New List(Of String)()
        For Each p As ParamC In cible.Parametres
            arguments.Add(Argument(ctx, p.Genre, rnd))
        Next

        Dim appel As String = cible.Nom & "(" & String.Join(", ", arguments) & ")"
        If cible.RetourneUnEntier() Then
            Return "status = " & appel & ";"
        End If
        Return appel & ";"
    End Function

    Private Shared Function Argument(ctx As ContexteFonction, genre As GenreParam, rnd As Random) As String
        Select Case genre
            Case GenreParam.Entier
                Return Operande(ctx, rnd)
            Case GenreParam.Reel
                If ctx.Reels.Count > 0 Then
                    Return ChoisirDans(ctx.Reels, rnd)
                End If
                Return "0.5"
            Case GenreParam.Chaine
                If ctx.Chaines.Count > 0 Then
                    Return ctx.Chaines(0)
                End If
                Return Guillemet & "default" & Guillemet
            Case Else
                If ctx.Enregistrements.Count > 0 Then
                    Return ctx.Enregistrements(0)
                End If
                Return "NULL"
        End Select
    End Function

    Private Shared Function Operande(ctx As ContexteFonction, rnd As Random) As String
        Select Case rnd.Next(0, 8)
            Case 0, 1, 2
                Return ChoisirDans(ctx.Entiers, rnd)
            Case 3
                If ctx.ProfondeurBoucle > 0 Then
                    Return Compteurs(ctx.ProfondeurBoucle - 1)
                End If
                Return rnd.Next(0, 100).ToString()
            Case 4
                Return ctx.Unite.PrefixeMajuscule & "_MAX_ITEMS"
            Case 5
                If ctx.Tampons.Count > 0 Then
                    Return "sizeof(" & ctx.Tampons(0) & ")"
                End If
                Return rnd.Next(1, 64).ToString()
            Case 6
                If ctx.Enregistrements.Count > 0 Then
                    Return ctx.Enregistrements(0) & "->" & ChoisirDans(ctx.Unite.ChampsEntiers, rnd)
                End If
                Return ChoisirDans(ctx.Entiers, rnd)
            Case Else
                If ctx.Chaines.Count > 0 Then
                    Return "strlen(" & ctx.Chaines(0) & ")"
                End If
                Return ChoisirDans(ctx.Entiers, rnd)
        End Select
    End Function

    Private Shared Function Expression(ctx As ContexteFonction, rnd As Random) As String
        Select Case rnd.Next(0, 8)
            Case 0, 1
                Return Operande(ctx, rnd)
            Case 2
                Return Operande(ctx, rnd) & " + " & Operande(ctx, rnd)
            Case 3
                Return Operande(ctx, rnd) & " - " & Operande(ctx, rnd)
            Case 4
                Return Operande(ctx, rnd) & " * " & rnd.Next(2, 9).ToString()
            Case 5
                Return "(" & Operande(ctx, rnd) & " + " & Operande(ctx, rnd) & ") / 2"
            Case 6
                Return Operande(ctx, rnd) & " & 0x" & rnd.Next(1, 256).ToString("X2")
            Case Else
                Return Operande(ctx, rnd) & " << " & rnd.Next(1, 4).ToString()
        End Select
    End Function

    Private Shared Function Condition(ctx As ContexteFonction, rnd As Random) As String
        Dim premiere As String = ConditionSimple(ctx, rnd)
        If rnd.Next(0, 5) = 0 Then
            Dim operateur As String = If(rnd.Next(2) = 0, " && ", " || ")
            Return premiere & operateur & ConditionSimple(ctx, rnd)
        End If
        Return premiere
    End Function

    Private Shared Function ConditionSimple(ctx As ContexteFonction, rnd As Random) As String
        Select Case rnd.Next(0, 9)
            Case 0
                Return ChoisirDans(ctx.Entiers, rnd) & " > 0"
            Case 1
                Return ChoisirDans(ctx.Entiers, rnd) & " < " & Operande(ctx, rnd)
            Case 2
                Return ChoisirDans(ctx.Entiers, rnd) & " == " & rnd.Next(0, 10).ToString()
            Case 3
                Return ChoisirDans(ctx.Entiers, rnd) & " != " & Operande(ctx, rnd)
            Case 4
                If ctx.Pointeurs.Count > 0 Then
                    Return ChoisirDans(ctx.Pointeurs, rnd) & " == NULL"
                End If
                Return ChoisirDans(ctx.Entiers, rnd) & " >= 0"
            Case 5
                If ctx.Pointeurs.Count > 0 Then
                    Return ChoisirDans(ctx.Pointeurs, rnd) & " != NULL"
                End If
                Return ChoisirDans(ctx.Entiers, rnd) & " < " & Operande(ctx, rnd)
            Case 6
                Return "(" & ChoisirDans(ctx.Entiers, rnd) & " & 0x01) != 0"
            Case 7
                If ctx.Chaines.Count > 0 Then
                    Return "strcmp(" & ctx.Chaines(0) & ", " & Guillemet & "stop" & Guillemet & ") == 0"
                End If
                Return ChoisirDans(ctx.Entiers, rnd) & " <= " & rnd.Next(1, 50).ToString()
            Case Else
                Return "!" & ChoisirDans(ctx.Entiers, rnd)
        End Select
    End Function

#End Region

End Class