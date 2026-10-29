Option Strict On
Option Explicit On


Imports System.Drawing

''' <summary>
''' Contrat du module Affichage (Etudiant 3), il est utilise par Interaction (Etudiant 4)
''' et par le Moteur (Etudiant 5)
''' </summary>

Public Interface IRenduBureau

    'Dessine uniquement les lignes visibles dans zoneVisible.
    'Graphics est l'objet de dessin de Windows Forms
    Sub Dessiner(g As Graphics, zoneVisible As Rectangle)

    'Renvoie la ligne situee a la position (x, y) a l'ecran, ou Nothing s'il n'y en a pas.
    Function ObtenirLigneALaPosition(x As Single, y As Single) As CodeLine

End Interface
