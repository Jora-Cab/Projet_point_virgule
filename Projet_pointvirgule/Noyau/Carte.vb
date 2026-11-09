Option Strict On
Option Explicit On

''' <summary>
''' Plan du batiment : # = mur, C = cloison de bureau (basse), . = sol libre.
''' </summary>
Public Class Carte

    Private ReadOnly _plan As String()

    Public ReadOnly Property Largeur As Integer
    Public ReadOnly Property Hauteur As Integer

    Public Sub New(plan As String())
        If plan Is Nothing OrElse plan.Length = 0 Then
            Throw New ArgumentException("Le plan est vide", "plan")
        End If
        _plan = plan
        Hauteur = plan.Length
        Largeur = plan(0).Length
    End Sub

    'Plan par defaut : 3 bureaux relies par un couloir
    Public Shared Function CreerParDefaut() As Carte
        Return New Carte(New String() {
            "##############################",
            "#........#........#..........#",
            "#........#...C.C..#..........#",
            "#..CCC...#........#....CC....#",
            "#........#...C.C..#..........#",
            "#..C.....#........#....CC....#",
            "#........#........#..........#",
            "####.#########.##########.####",
            "#.........#.....C............#",
            "#.........#..................#",
            "#.....................C......#",
            "#.........#..................#",
            "##############################"
        })
    End Function

    'Quel caractere se trouve sur cette case ? (hors plan = mur)
    Public Function TypeCase(x As Integer, y As Integer) As Char
        If x < 0 OrElse y < 0 OrElse x >= Largeur OrElse y >= Hauteur Then
            Return "#"c
        End If
        Return _plan(y)(x)
    End Function

    'Cette case bloque-t-elle le passage ?
    Public Function EstPlein(x As Integer, y As Integer) As Boolean
        Dim c As Char = TypeCase(x, y)
        Return c = "#"c OrElse c = "C"c
    End Function

End Class