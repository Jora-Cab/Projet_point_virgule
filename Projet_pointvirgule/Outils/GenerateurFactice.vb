Option Strict On
Option Explicit On

''' <summary>
''' TEMPORAIRE : faux generateur pour tester sans attendre le vrai GenerateurCode.
''' Produit du code de style JavaScript dont chaque ligne finit par un point-virgule.
''' </summary>
Public Class GenerateurFactice
    Implements IGenerateurCode

    'Modeles de lignes : le # sera remplace par le numero de la ligne
    Private ReadOnly _modeles As String() = {
        "const logger# = require('../lib/logger');",
        "    var total = total + #;",
        "    logger.debug('etape #', { ok: true });",
        "    return cb(null, data#);",
        "    acc += Number(row#.accountId || 0);",
        "module.exports = { helper# };"
    }

    Public Function GenererLignesValides(nombre As Integer) As List(Of CodeLine) _
        Implements IGenerateurCode.GenererLignesValides
        Dim lignes As New List(Of CodeLine)
        For i As Integer = 1 To nombre
            Dim modele As String = _modeles((i - 1) Mod _modeles.Length)
            lignes.Add(New CodeLine(i, modele.Replace("#", i.ToString())))
        Next
        Return lignes
    End Function

    Public Function CasserLigne(ligne As CodeLine) As CodeLine _
        Implements IGenerateurCode.CasserLigne
        Dim contenu As String = ligne.Contenu.TrimEnd(";"c)
        Return New CodeLine(ligne.NumeroLigne, contenu, True)
    End Function

End Class
