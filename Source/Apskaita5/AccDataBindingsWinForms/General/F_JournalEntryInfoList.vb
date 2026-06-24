Imports AccControlsWinForms
Imports ApskaitaObjects.ActiveReports
Imports AccDataBindingsWinForms.CachedInfoLists
Imports AccDataBindingsWinForms.Printing
Imports ApskaitaObjects.Attributes
Imports ApskaitaObjects.Documents
Imports ApskaitaObjects.HelperLists

Friend Class F_JournalEntryInfoList
    Implements ISupportsPrinting

    Private ReadOnly _RequiredCachedLists As Type() = New Type() _
        {GetType(HelperLists.AccountInfoList), GetType(HelperLists.PersonInfoList),
        GetType(HelperLists.PersonGroupInfoList)}

    Private _TypesAbleToDelete As DocumentType() = New DocumentType() _
        {DocumentType.Offset, DocumentType.AccumulatedCosts,
        DocumentType.TransferOfBalance, DocumentType.HolidayReserve,
        DocumentType.None, DocumentType.ClosingEntries, DocumentType.BankOperation, DocumentType.TillIncomeOrder,
        DocumentType.TillSpendingOrder, DocumentType.InvoiceMade, DocumentType.InvoiceReceived,
        DocumentType.WageSheet, DocumentType.AdvanceReport, DocumentType.ImprestSheet}

    Private _FormManager As CslaActionExtenderReportForm(Of JournalEntryInfoList)
    Private _ListViewManager As DataListViewEditControlManager(Of JournalEntryInfo)
    Private _QueryManager As CslaActionExtenderQueryObject
    Private _DeleteSuccessMessage As String = ""

    Private _SelectedEntries As List(Of JournalEntryInfo) = Nothing
    Private _DateFrom As Date = Today
    Private _DateTo As Date = Today
    Private _OnlyAllowSingleSelection As Boolean = False


    Public ReadOnly Property SelectedEntries() As List(Of JournalEntryInfo)
        Get
            Return _SelectedEntries
        End Get
    End Property


    Public Sub New()

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

    End Sub

    ''' <summary>
    ''' Use to show the form as modal dialog for journal entry info selection.
    ''' </summary>
    ''' <param name="dateFrom">prefered period start date</param>
    ''' <param name="dateTo">prefered period end date</param>
    ''' <param name="onlyAllowSingleSelection">whether only to allow user to select a single operation 
    ''' (not multiple)</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal dateFrom As Date, ByVal dateTo As Date, ByVal onlyAllowSingleSelection As Boolean)

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _DateFrom = dateFrom
        _DateTo = dateTo
        _OnlyAllowSingleSelection = onlyAllowSingleSelection

    End Sub


    Private Sub F_GeneralLedger_Load(ByVal sender As System.Object,
        ByVal e As System.EventArgs) Handles MyBase.Load

        If Not SetDataSources() Then Exit Sub

        Me.Panel1.Visible = Me.Modal
        Me.CloseAccountsButton.Visible = Not Me.Modal
        Me.ItemsDataListView.CheckBoxes = Me.Modal
        Me.MinimizeBox = Not Me.Modal
        Me.MaximizeBox = Not Me.Modal

        If Me.Modal Then
            If _OnlyAllowSingleSelection Then
                Me.Text = "Pasirinkite bendrojo žurnalo operaciją..."
            Else
                Me.Text = "Pasirinkite bendrojo žurnalo operacijas..."
            End If
        End If

        If Me.Modal AndAlso Me.WindowState = FormWindowState.Maximized Then Me.WindowState = FormWindowState.Normal

    End Sub

    Private Function SetDataSources() As Boolean

        If Not PrepareCache(Me, _RequiredCachedLists) Then Return False

        Try

            If Me.Modal Then

                _ListViewManager = New DataListViewEditControlManager(Of JournalEntryInfo) _
                    (ItemsDataListView, Nothing, Nothing, Nothing, Nothing, Nothing)

                _ListViewManager.AddCancelButton = False
                _ListViewManager.AddButtonHandler("Pasirinkti", "Pasirinkti", AddressOf SelectItem)

            Else

                _ListViewManager = New DataListViewEditControlManager(Of JournalEntryInfo) _
                (ItemsDataListView, ContextMenuStrip1, Nothing, Nothing,
                 AddressOf ItemActionIsAvailable, Nothing)

                _ListViewManager.AddCancelButton = True
                _ListViewManager.AddButtonHandler(My.Resources.F_JournalEntryInfoList_EditLabel,
                    My.Resources.F_JournalEntryInfoList_EditTooltipLabel, AddressOf EditItem)
                _ListViewManager.AddButtonHandler(My.Resources.F_JournalEntryInfoList_CopyJournalEntryLabel,
                    My.Resources.F_JournalEntryInfoList_CopyJournalEntryToolTipLabel, AddressOf CopyJournalEntry)
                _ListViewManager.AddButtonHandler(My.Resources.F_JournalEntryInfoList_DeleteLabel,
                    My.Resources.F_JournalEntryInfoList_DeleteTooltipLabel, AddressOf DeleteItem)
                _ListViewManager.AddButtonHandler(My.Resources.F_JournalEntryInfoList_CorrespondencesLabel,
                    My.Resources.F_JournalEntryInfoList_CorrespondencesTooltipLabel, AddressOf ShowItemJournalEntry)
                _ListViewManager.AddButtonHandler(My.Resources.F_JournalEntryInfoList_RelationsLabel,
                    My.Resources.F_JournalEntryInfoList_RelationsTooltipLabel, AddressOf ShowItemRelations)

                _ListViewManager.AddMenuItemHandler(EditToolStripMenuItem, AddressOf EditItem)
                _ListViewManager.AddMenuItemHandler(CopyJournalEntryToolStripMenuItem, AddressOf CopyJournalEntry)
                _ListViewManager.AddMenuItemHandler(DeleteToolStripMenuItem, AddressOf DeleteItem)
                _ListViewManager.AddMenuItemHandler(CorrespondencesToolStripMenuItem, AddressOf ShowItemJournalEntry)
                _ListViewManager.AddMenuItemHandler(RelationsToolStripMenuItem, AddressOf ShowItemRelations)

            End If

            _FormManager = New CslaActionExtenderReportForm(Of JournalEntryInfoList) _
                (Me, JournalEntryInfoListBindingSource, Nothing, _RequiredCachedLists,
                 RefreshButton, ProgressFiller1, "GetList", AddressOf GetReportParams)

            _FormManager.ManageDataListViewStates(ItemsDataListView)

            _QueryManager = New CslaActionExtenderQueryObject(Me, ProgressFiller2)

            PrepareControl(PersonGroupComboBox, New PersonGroupFieldAttribute(
                ValueRequiredLevel.Optional))
            PrepareControl(PersonAccGridComboBox, New PersonFieldAttribute(
                ValueRequiredLevel.Optional, True, True, True, False))
            PrepareControl(DocTypeComboBox, New LocalizedEnumFieldAttribute(
                GetType(DocumentType), True, "Visi tipai"))

        Catch ex As Exception
            ShowError(ex, Nothing)
            DisableAllControls(Me)
            Return False
        End Try

        If Me.Modal Then
            Me.DateFromAccDatePicker.Value = _DateFrom
            Me.DateToAccDatePicker.Value = _DateTo
        Else
            DateFromAccDatePicker.Value = Today.AddMonths(-3)
        End If

        Return True

    End Function


    Private Function GetReportParams() As Object()

        Dim person As PersonInfo = TryCast(PersonAccGridComboBox.SelectedValue, PersonInfo)
        If person Is Nothing Then person = PersonInfo.Empty()

        Dim group As PersonGroupInfo = TryCast(PersonGroupComboBox.SelectedValue, PersonGroupInfo)
        If group Is Nothing Then group = PersonGroupInfo.Empty()

        Dim documentFilter As DocumentType = DocumentType.None
        Dim applyDocFilter As Boolean = False
        If DocTypeComboBox.SelectedIndex > 0 Then
            applyDocFilter = True
            documentFilter = ConvertLocalizedName(Of DocumentType)(DocTypeComboBox.SelectedItem.ToString)
        End If

        Return New Object() {DateFromAccDatePicker.Value.Date, DateToAccDatePicker.Value.Date,
            ContentTextBox.Text.Trim, person.ID, group.ID, documentFilter, applyDocFilter,
            person.Name, group.Name}

    End Function

    Private Function ItemActionIsAvailable(ByVal item As JournalEntryInfo,
        ByVal action As String) As Boolean

        If item Is Nothing OrElse action Is Nothing Then Return False

        If action.Trim.ToLower = "EditItem".ToLower Then

            If item.DocType = DocumentType.ClosingEntries Then
                Return False
            Else
                Return True
            End If

        ElseIf action.Trim.ToLower = "DeleteItem".ToLower Then

            If Array.IndexOf(_TypesAbleToDelete, item.DocType) < 0 Then
                Return False
            Else
                Return True
            End If

        ElseIf action.Trim.ToLower = "ShowItemJournalEntry".ToLower Then

            If item.DocType = DocumentType.None Then
                Return False
            Else
                Return True
            End If

        ElseIf action.Trim.ToLower = "ShowItemJournalEntry".ToLower Then

            Return (item.DocType = DocumentType.None)

        ElseIf action.Trim.ToLower = "ShowItemRelations".ToLower Then
            Return True

        ElseIf action.Trim.ToLower = "CopyJournalEntry".ToLower Then
            Return item.DocType = DocumentType.None OrElse
                   item.DocType = DocumentType.InvoiceMade OrElse
                   item.DocType = DocumentType.InvoiceReceived
        End If

        Return False

    End Function

    Private Sub EditItem(ByVal item As JournalEntryInfo)
        OpenObjectEditForm(_QueryManager, item.Id, item.DocType)
    End Sub

    Private Sub DeleteItem(ByVal item As JournalEntryInfo)

        If item.DocType = DocumentType.ClosingEntries Then
            If Not YesOrNo("Ar tikrai norite pašalinti 5/6 klasių uždarymo duomenis iš duomenų bazės?") Then Exit Sub
        ElseIf item.DocType = DocumentType.None Then
            If Not YesOrNo("Ar tikrai norite pašalinti bendrojo žurnalo įrašo duomenis iš duomenų bazės?") Then Exit Sub
        Else
            If Not YesOrNo("Ar tikrai norite pašalinti dokumento duomenis iš duomenų bazės?") Then Exit Sub
        End If

        _DeleteSuccessMessage = "Dokumento duomenys sėkmingai pašalinti iš įmonės duomenų bazės."
        If item.DocType = DocumentType.ClosingEntries Then
            _DeleteSuccessMessage = "5/6 klasių uždarymo duomenys sėkmingai pašalinti iš įmonės duomenų bazės."
        ElseIf item.DocType = DocumentType.None Then
            _DeleteSuccessMessage = "Bendojo žurnalo įrašo duomenys sėkmingai pašalinti iš įmonės duomenų bazės."
        End If

        If item.DocType = DocumentType.Offset Then
            _QueryManager.InvokeQuery(Of Documents.Offset)(Nothing, "DeleteOffset", False,
                AddressOf OnItemDeleted, item.Id)
        ElseIf item.DocType = DocumentType.AccumulatedCosts Then
            _QueryManager.InvokeQuery(Of Documents.AccumulativeCosts)(Nothing, "DeleteAccumulativeCosts", False,
                AddressOf OnItemDeleted, item.Id)
        ElseIf item.DocType = DocumentType.TransferOfBalance Then
            _QueryManager.InvokeQuery(Of General.TransferOfBalance)(Nothing, "DeleteTransferOfBalance", False,
                AddressOf OnItemDeleted)
        ElseIf item.DocType = DocumentType.HolidayReserve Then
            _QueryManager.InvokeQuery(Of Workers.HolidayPayReserve)(Nothing, "DeleteHolidayPayReserve", False,
                AddressOf OnItemDeleted, item.Id)
        ElseIf item.DocType = DocumentType.BankOperation Then
            'BankOperation.DeleteBankOperation(item.ID)
            _QueryManager.InvokeQuery(Of BankOperation)(Nothing, "DeleteBankOperation", True,
                AddressOf OnItemDeleted, item.Id)
        ElseIf item.DocType = DocumentType.TillIncomeOrder Then
            'TillIncomeOrder.DeleteTillIncomeOrder(item.ID)
            _QueryManager.InvokeQuery(Of TillIncomeOrder)(Nothing, "DeleteTillIncomeOrder", True,
                AddressOf OnItemDeleted, item.Id)
        ElseIf item.DocType = DocumentType.TillSpendingOrder Then
            'TillSpendingsOrder.DeleteTillSpendingsOrder(item.ID)
            _QueryManager.InvokeQuery(Of TillSpendingsOrder)(Nothing, "DeleteTillSpendingsOrder", True,
                AddressOf OnItemDeleted, item.Id)
        ElseIf item.DocType = InvoiceInfoType.InvoiceMade Then
            'InvoiceMade.DeleteInvoiceMade(item.ID)
            _QueryManager.InvokeQuery(Of InvoiceMade)(Nothing, "DeleteInvoiceMade", False,
                AddressOf OnItemDeleted, item.Id)
        ElseIf item.DocType = InvoiceInfoType.InvoiceReceived Then
            'InvoiceReceived.DeleteInvoiceReceived(item.ID)
            _QueryManager.InvokeQuery(Of InvoiceReceived)(Nothing, "DeleteInvoiceReceived", False,
                AddressOf OnItemDeleted, item.Id)
        ElseIf item.DocType = DocumentType.AdvanceReport Then
            ' AdvanceReport.DeleteAdvanceReport(item.ID)
            _QueryManager.InvokeQuery(Of AdvanceReport)(Nothing, "DeleteAdvanceReport", False,
                AddressOf OnItemDeleted, item.Id)
        ElseIf item.DocType = DocumentType.WageSheet Then
            ' WageSheet.DeleteWageSheet(item.ID)
            _QueryManager.InvokeQuery(Of Workers.WageSheet)(Nothing, "DeleteWageSheet", False,
                AddressOf OnItemDeleted, item.Id)
        ElseIf item.DocType = DocumentType.ImprestSheet Then
            ' ImprestSheet.DeleteImprestSheet(item.ID)
            _QueryManager.InvokeQuery(Of Workers.ImprestSheet)(Nothing, "DeleteImprestSheet", False,
                AddressOf OnItemDeleted, item.Id)
        Else
            _QueryManager.InvokeQuery(Of General.JournalEntry)(Nothing, "DeleteJournalEntry", False, _
                AddressOf OnItemDeleted, item.Id)
        End If

    End Sub

    Private Sub OnItemDeleted(ByVal nullResult As Object, ByVal exceptionHandled As Boolean)
        If exceptionHandled Then Exit Sub
        If Not YesOrNo(_DeleteSuccessMessage & vbCrLf & "Atnaujinti ataskaitos duomenis?") Then Exit Sub
        RefreshButton.PerformClick()
    End Sub

    Private Sub ShowItemJournalEntry(ByVal item As JournalEntryInfo)
        OpenJournalEntryEditForm(_QueryManager, item.Id)
    End Sub

    Private Sub CopyJournalEntry(ByVal item As JournalEntryInfo)
        If item Is Nothing OrElse Not item.Id > 0 Then Exit Sub
        If item.DocType = DocumentType.None Then
            _QueryManager.InvokeQuery(Of General.JournalEntry)(Nothing, "GetJournalEntry", True,
                AddressOf OnJournalEntryFetched, item.Id)
        ElseIf item.DocType = DocumentType.InvoiceMade Then
            _QueryManager.InvokeQuery(Of InvoiceMade)(Nothing, "GetInvoiceMade", True,
                AddressOf OnInvoiceFetched, item.Id)
        ElseIf item.DocType = DocumentType.InvoiceReceived Then
            _QueryManager.InvokeQuery(Of InvoiceReceived)(Nothing, "GetInvoiceReceived", True,
                AddressOf OnInvoiceFetched, item.Id)
        End If
    End Sub

    Private Sub OnJournalEntryFetched(ByVal result As Object, ByVal exceptionHandled As Boolean)
        If result Is Nothing Then Exit Sub
        OpenObjectEditForm(DirectCast(result, General.JournalEntry).GetJournalEntryCopy())
    End Sub

    Private Sub OnInvoiceFetched(ByVal result As Object, ByVal exceptionHandled As Boolean)
        If result Is Nothing Then Exit Sub
        Try
            If TypeOf result Is InvoiceMade Then
                OpenObjectEditForm(DirectCast(result, InvoiceMade).GetInvoiceMadeCopy())
            ElseIf TypeOf result Is InvoiceReceived Then
                OpenObjectEditForm(DirectCast(result, InvoiceReceived).GetInvoiceReceivedCopy())
            End If
        Catch ex As Exception
            ShowError(ex, result)
        End Try
    End Sub

    Private Sub ShowItemRelations(ByVal item As JournalEntryInfo)
        If item Is Nothing Then Exit Sub
        _QueryManager.InvokeQuery(Of HelperLists.IndirectRelationInfoList) _
            (Nothing, "GetIndirectRelationInfoList", True, _
             AddressOf OpenObjectEditForm, item.Id)
    End Sub

    Private Sub SelectItem(ByVal item As JournalEntryInfo)

        If item Is Nothing Then Exit Sub

        _SelectedEntries = New List(Of JournalEntryInfo)
        _SelectedEntries.Add(item)

        Me.DialogResult = DialogResult.OK
        Me.Close()

    End Sub



    Private Sub ClearButton_Click(ByVal sender As System.Object, _
        ByVal e As System.EventArgs) Handles ClearButton.Click
        DocTypeComboBox.SelectedIndex = -1
        PersonGroupComboBox.SelectedValue = Nothing
        PersonAccGridComboBox.SelectedValue = Nothing
        ContentTextBox.Text = ""
    End Sub

    Private Sub CloseAccountsButton_Click(ByVal sender As System.Object, _
        ByVal e As System.EventArgs) Handles CloseAccountsButton.Click
        OpenNewForm(Of General.ClosingEntriesCommand)()
    End Sub

    Private Sub nOkButton_Click(ByVal sender As System.Object, _
        ByVal e As System.EventArgs) Handles nOkButton.Click

        If Not Me.Modal Then Exit Sub

        If Me.ItemsDataListView.CheckedObjects Is Nothing OrElse Me.ItemsDataListView.CheckedObjects.Count < 1 Then
            MsgBox("Klaida. Nepasirinkta nė viena operacija.", MsgBoxStyle.Exclamation, "Klaida.")
            Exit Sub
        ElseIf _OnlyAllowSingleSelection AndAlso Me.ItemsDataListView.CheckedObjects.Count > 1 Then
            MsgBox("Klaida. Leidžiama pasirinkti tik vieną operaciją.", MsgBoxStyle.Exclamation, "Klaida.")
            Exit Sub
        End If

        _SelectedEntries = New List(Of JournalEntryInfo)
        For Each selectedItem As Object In Me.ItemsDataListView.CheckedObjects
            _SelectedEntries.Add(DirectCast(selectedItem, JournalEntryInfo))
        Next

        Me.DialogResult = DialogResult.OK
        Me.Close()

    End Sub

    Private Sub nCancelButton_Click(ByVal sender As System.Object, _
        ByVal e As System.EventArgs) Handles nCancelButton.Click
        If Not Me.Modal Then Exit Sub
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub


    Public Function GetMailDropDownItems() As System.Windows.Forms.ToolStripDropDown _
        Implements ISupportsPrinting.GetMailDropDownItems
        Return Nothing
    End Function

    Public Function GetPrintDropDownItems() As System.Windows.Forms.ToolStripDropDown _
        Implements ISupportsPrinting.GetPrintDropDownItems
        Return Nothing
    End Function

    Public Function GetPrintPreviewDropDownItems() As System.Windows.Forms.ToolStripDropDown _
        Implements ISupportsPrinting.GetPrintPreviewDropDownItems
        Return Nothing
    End Function

    Public Sub OnMailClick(ByVal sender As Object, ByVal e As System.EventArgs) _
        Implements ISupportsPrinting.OnMailClick
        If _FormManager.DataSource Is Nothing Then Exit Sub

        Using frm As New F_SendObjToEmail(_FormManager.DataSource, 0)
            frm.ShowDialog()
        End Using

    End Sub

    Public Sub OnPrintClick(ByVal sender As Object, ByVal e As System.EventArgs) _
        Implements ISupportsPrinting.OnPrintClick
        If _FormManager.DataSource Is Nothing Then Exit Sub
        Try
            PrintObject(_FormManager.DataSource, False, 0, "BendrasisZurnalas", Me, _
                _ListViewManager.GetCurrentFilterDescription(), _ListViewManager.GetDisplayOrderIndexes())
        Catch ex As Exception
            ShowError(ex, _FormManager.DataSource)
        End Try
    End Sub

    Public Sub OnPrintPreviewClick(ByVal sender As Object, ByVal e As System.EventArgs) _
        Implements ISupportsPrinting.OnPrintPreviewClick
        If _FormManager.DataSource Is Nothing Then Exit Sub
        Try
            PrintObject(_FormManager.DataSource, True, 0, "BendrasisZurnalas", Me, _
                _ListViewManager.GetCurrentFilterDescription(), _ListViewManager.GetDisplayOrderIndexes())
        Catch ex As Exception
            ShowError(ex, _FormManager.DataSource)
        End Try
    End Sub

    Public Function SupportsEmailing() As Boolean _
        Implements ISupportsPrinting.SupportsEmailing
        Return True
    End Function

End Class