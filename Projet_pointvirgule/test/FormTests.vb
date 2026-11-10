Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms

''' <summary>
''' Fenetre qui lance les tests automatiques de l'Etudiant 5 (touche F12 dans le jeu)
''' et affiche le resultat. Elle est construite par le code : pas de fichier Designer.
''' </summary>
Public Class FormTests
    Inherits Form

    Private ReadOnly _texte As New TextBox()
    Private ReadOnly _bouton As New Button()

    Public Sub New()
        Me.Text = "Tests automatiques - Etudiant 5"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.ClientSize = New Size(920, 580)

        _texte.Multiline = True
        _texte.ReadOnly = True
        _texte.ScrollBars = ScrollBars.Vertical
        _texte.Font = New Font("Consolas", 10)
        _texte.Location = New Point(0, 0)
        _texte.Size = New Size(920, 540)
        _texte.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right

        _bouton.Text = "Relancer les tests"
        _bouton.Location = New Point(0, 544)
        _bouton.Size = New Size(920, 36)
        _bouton.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        AddHandler _bouton.Click, AddressOf OnRelancer

        Me.Controls.Add(_texte)
        Me.Controls.Add(_bouton)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        Lancer()
    End Sub

    Private Sub OnRelancer(sender As Object, e As EventArgs)
        Lancer()
    End Sub

    Private Sub Lancer()
        _texte.Text = "Execution des tests en cours (le test de performance peut prendre quelques secondes)..."
        _bouton.Enabled = False
        Me.Cursor = Cursors.WaitCursor
        Application.DoEvents()

        Dim resultats As List(Of ResultatTest) = TestsEtudiant5.Executer()

        Dim sb As New StringBuilder()
        Dim reussis As Integer = 0
        For Each r As ResultatTest In resultats
            If r.Reussi Then
                reussis += 1
                sb.AppendLine("[OK]     " & r.Nom & "   (" & r.DureeMs.ToString() & " ms)")
            Else
                sb.AppendLine("[ECHEC]  " & r.Nom)
                sb.AppendLine("         -> " & r.Message)
            End If
            If r.Detail <> "" Then
                sb.AppendLine("         " & r.Detail)
            End If
        Next
        sb.AppendLine()
        sb.AppendLine(reussis.ToString() & " test(s) reussi(s) sur " & resultats.Count.ToString())

        _texte.Text = sb.ToString()
        _bouton.Enabled = True
        Me.Cursor = Cursors.Default
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()
        '
        'FormTests
        '
        Me.ClientSize = New System.Drawing.Size(278, 244)
        Me.Name = "FormTests"
        Me.ResumeLayout(False)

    End Sub

    Private Sub FormTests_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
End Class
