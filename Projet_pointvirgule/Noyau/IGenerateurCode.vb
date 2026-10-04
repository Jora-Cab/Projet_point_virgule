Option Strict On
Option Explicit On

''' <summary>
''' Contrat du module Generation
''' </summary>

Public Interface IGenerateurCode
    ''' <summary>Genere "nombre" lignes valides, numerotees a partir de 1.</summary>
    Function GenererLignesValides(nombre As Integer) As List(Of CodeLine)

    ''' <summary> Renvoie une nouvelle ligne sans point virgule, avec EstErreur = True.</summary>
    Function CasserLigne(ligne As CodeLine) As CodeLine

End Interface
