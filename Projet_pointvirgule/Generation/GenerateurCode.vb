Option Strict On
Option Explicit On

''' <summary>
''' Genere les lignes de code credibles et "casse" une ligne
''' Choix aleatoire de la ligne fautive et serialisation ( sauvegarde et lecture dans un fichier)
''' </summary>

Class GenerateurCode
    Implements IGenerateurCode


    'Methode de IGenerateurCode : generer des lignes valides
    Public Function GenererLignesValides(nombre As Integer) As List(Of CodeLine) Implements IGenerateurCode.GenererLignesValides
        'Etudiant 2 : vocabulaire de mot cles, variation d'indentation
        Throw New NotImplementedException("A faire par l'etudiant 2")
    End Function

    'Methode de IGenerateurCode : casser une ligne 
    Public Function CasserLigne(ligne As CodeLine) As CodeLine Implements IGenerateurCode.CasserLigne
        'Etudiant 2 : retirer le point virgule
        'puis renvoyer une nouvelle CodeLine avec EstErreur = True
        Throw New NotImplementedException("A faire par l'etudiant 2")
    End Function


End Class
