Public Class Form1

    Private _vue As VuePiece

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.ClientSize = New Size(960, 540)

        _vue = New VuePiece()
        _vue.Dock = DockStyle.Fill
        _vue.Distance = 8
        Me.Controls.Add(_vue)
    End Sub

    'Capte les fleches du clavier
    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If _vue IsNot Nothing Then
            Select Case keyData
                Case Keys.Up
                    _vue.Avancer()
                    Return True
                Case Keys.Down
                    _vue.Reculer()
                    Return True
                Case Keys.Left
                    _vue.TournerGauche()
                    Return True
                Case Keys.Right
                    _vue.TournerDroite()
                    Return True
            End Select
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

End Class