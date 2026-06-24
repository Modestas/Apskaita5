Imports ApskaitaObjects.Attributes
Imports ApskaitaObjects.My.Resources

Namespace General

    ''' <summary>
    ''' Represents a single ledger operation that encompasses two or more <see cref="general.BookEntry">transactions</see>.
    ''' </summary>
    ''' <remarks>Values are stored in the database table bz.</remarks>
    <Serializable()> _
    Public NotInheritable Class JournalEntry
        Inherits BusinessBase(Of JournalEntry)
        Implements IIsDirtyEnough, IValidationMessageProvider

#Region " Business Methods "

        Private _Guid As Guid = Guid.NewGuid
        Private _ID As Integer = 0
        Private _Date As Date = Today
        Private _DocNumber As String = ""
        Private _Content As String = ""
        Private _Person As PersonInfo = Nothing
        Private _DocType As DocumentType = DocumentType.None
        Private _PluginDocTypeCode As String = ""
        Private _DocTypeHumanReadable As String = ""
        Private WithEvents _DebetList As BookEntryList
        Private WithEvents _CreditList As BookEntryList
        Private _DebetSum As Double = 0
        Private _CreditSum As Double = 0
        Private _InsertDate As DateTime = Now
        Private _UpdateDate As DateTime = Now
        Private _ChronologyValidator As IChronologicValidator = Nothing

        ' used to implement automatic sort in datagridview
        <NotUndoable()> _
        <NonSerialized()> _
        Private _DebetListSortedList As Csla.SortedBindingList(Of BookEntry) = Nothing
        ' used to implement automatic sort in datagridview
        <NotUndoable()> _
        <NonSerialized()> _
        Private _CreditListSortedList As Csla.SortedBindingList(Of BookEntry) = Nothing

        ''' <summary>
        ''' Gets an ID of the JournalEntry object (assigned by DB AUTO_INCREMENT).
        ''' </summary>
        ''' <remarks>Value is stored in the database field bz.Op_ID.</remarks>
        Public ReadOnly Property ID() As Integer
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return _ID
            End Get
        End Property

        ''' <summary>
        ''' Gets the date and time when the JournalEntry was inserted into the database.
        ''' </summary>
        ''' <remarks>Value is stored in the database field bz.InsertDate.</remarks>
        Public ReadOnly Property InsertDate() As DateTime
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return _InsertDate
            End Get
        End Property

        ''' <summary>
        ''' Gets the date and time when the JournalEntry was last updated.
        ''' </summary>
        ''' <remarks>Value is stored in the database field bz.UpdateDate.</remarks>
        Public ReadOnly Property UpdateDate() As DateTime
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return _UpdateDate
            End Get
        End Property

        ''' <summary>
        ''' Gets <see cref="IChronologicValidator">IChronologicValidator</see> object that contains business restraints on updating the operation.
        ''' </summary>
        ''' <remarks>A <see cref="SimpleChronologicValidator">SimpleChronologicValidator</see> is used to validate a journal entry chronological business rules.</remarks>
        Public ReadOnly Property ChronologyValidator() As IChronologicValidator
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return _ChronologyValidator
            End Get
        End Property

        ''' <summary>
        ''' Gets or sets a date of the Journal Entry.
        ''' </summary>
        ''' <remarks>Value is stored in the database field bz.Op_data.</remarks>
        Public Property [Date]() As Date
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return _Date
            End Get
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Set(ByVal value As Date)
                CanWriteProperty(True)
                If _Date.Date <> value.Date Then
                    _Date = value.Date
                    PropertyHasChanged()
                End If
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets a number of the document associated with the Journal Entry.
        ''' </summary>
        ''' <remarks>Value is stored in the database field bz.Op_Dok.</remarks>
        <StringField(ValueRequiredLevel.Mandatory, 30, False)> _
        Public Property DocNumber() As String
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return _DocNumber.Trim
            End Get
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Set(ByVal value As String)
                CanWriteProperty(True)
                If value Is Nothing Then value = ""
                If _DocNumber.Trim <> value.Trim Then
                    _DocNumber = value.Trim
                    PropertyHasChanged()
                End If
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets a content/description of the the Journal Entry.
        ''' </summary>
        ''' <remarks>Value is stored in the database field bz.Op_turinys.</remarks>
        <StringField(ValueRequiredLevel.Mandatory, 255, False)> _
        Public Property Content() As String
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return _Content.Trim
            End Get
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Set(ByVal value As String)
                CanWriteProperty(True)
                If value Is Nothing Then value = ""
                If _Content.Trim <> value.Trim Then
                    _Content = value.Trim
                    PropertyHasChanged()
                End If
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets a person associated with the Journal Entry.
        ''' </summary>
        ''' <remarks>Use <see cref="HelperLists.PersonInfoList">PersonInfoList</see> for the datasource.
        ''' Value is stored in the database field bz.Op_analitika.</remarks>
        <PersonField(ValueRequiredLevel.Recommended)> _
        Public Property Person() As PersonInfo
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return _Person
            End Get
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Set(ByVal value As PersonInfo)
                CanWriteProperty(True)
                If Not (_Person Is Nothing AndAlso value Is Nothing) _
                    AndAlso Not (Not _Person Is Nothing AndAlso Not value Is Nothing AndAlso _Person = value) Then
                    _Person = value
                    PropertyHasChanged()
                End If
            End Set
        End Property

        ''' <summary>
        ''' Gets a <see cref="DocumentType">DocumentType</see> of the document associated with the Journal Entry (as enum).
        ''' </summary>
        ''' <remarks>Value is stored in the database field bz.Op_dok_rusis.</remarks>
        Public ReadOnly Property DocType() As DocumentType
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return _DocType
            End Get
        End Property

        ''' <summary>
        ''' Gets a type code of the external plugin document associated with the Journal Entry 
        ''' (as a human readable string).
        ''' </summary>
        ''' <remarks>Value is stored in the database field bz.Op_dok_rusis.</remarks>
        Public ReadOnly Property PluginDocTypeCode() As String
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return _PluginDocTypeCode.Trim
            End Get
        End Property

        ''' <summary>
        ''' Gets a <see cref="DocumentType">DocumentType</see> of the document associated with the Journal Entry 
        ''' (as a human readable string).
        ''' </summary>
        ''' <remarks>Value is stored in the database field bz.Op_dok_rusis.</remarks>
        Public ReadOnly Property DocTypeHumanReadable() As String
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return _DocTypeHumanReadable.Trim
            End Get
        End Property

        ''' <summary>
        ''' Gets a BookEntryList of type Debit in the JuornalEntry. 
        ''' </summary>
        Public ReadOnly Property DebetList() As BookEntryList
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return _DebetList
            End Get
        End Property

        ''' <summary>
        ''' Gets a BookEntryList of type Credit in the JournalEntry. 
        ''' </summary>
        Public ReadOnly Property CreditList() As BookEntryList
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return _CreditList
            End Get
        End Property

        ''' <summary>
        ''' Gets a sortable BookEntryList of type Debet in the JuornalEntry. 
        ''' </summary>
        Public ReadOnly Property DebetListSorted() As Csla.SortedBindingList(Of BookEntry)
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                If _DebetListSortedList Is Nothing Then _DebetListSortedList = _
                    New Csla.SortedBindingList(Of BookEntry)(_DebetList)
                Return _DebetListSortedList
            End Get
        End Property

        ''' <summary>
        ''' Gets a sortable BookEntryList of type Credit in the JuornalEntry. 
        ''' </summary>
        Public ReadOnly Property CreditListSorted() As Csla.SortedBindingList(Of BookEntry)
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                If _CreditListSortedList Is Nothing Then _CreditListSortedList = _
                    New Csla.SortedBindingList(Of BookEntry)(_CreditList)
                Return _CreditListSortedList
            End Get
        End Property

        ''' <summary>
        ''' Gets a total sum of ammounts/values of all the BookEntryList of type Debit in the JuornalEntry. 
        ''' </summary>
        <DoubleField(ValueRequiredLevel.Mandatory, False, 2)> _
        Public ReadOnly Property DebetSum() As Double
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return CRound(_DebetSum)
            End Get
        End Property

        ''' <summary>
        ''' Gets a total sum of ammounts/values of all the BookEntryList of type Credit in the JuornalEntry. 
        ''' </summary>
        <DoubleField(ValueRequiredLevel.Mandatory, False, 2)> _
        Public ReadOnly Property CreditSum() As Double
            <System.Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)> _
            Get
                Return CRound(_CreditSum)
            End Get
        End Property


        ''' <summary>
        ''' Returnes TRUE if the object is new and contains some user provided data 
        ''' OR
        ''' object is not new and was changed by the user.
        ''' </summary>
        Public ReadOnly Property IsDirtyEnough() As Boolean _
            Implements IIsDirtyEnough.IsDirtyEnough
            Get
                If Not IsNew Then Return IsDirty
                Return (Not String.IsNullOrEmpty(_DocNumber.Trim) _
                    OrElse Not String.IsNullOrEmpty(_Content.Trim) _
                    OrElse _DebetList.Count > 0 OrElse _CreditList.Count > 0)
            End Get
        End Property

        Public Overrides ReadOnly Property IsDirty() As Boolean
            Get
                Return MyBase.IsDirty OrElse _DebetList.IsDirty OrElse _CreditList.IsDirty
            End Get
        End Property

        Public Overrides ReadOnly Property IsValid() As Boolean _
            Implements IValidationMessageProvider.IsValid
            Get
                Return MyBase.IsValid AndAlso _DebetList.IsValid AndAlso _CreditList.IsValid
            End Get
        End Property


        Public Overrides Function Save() As JournalEntry

            If Not Me.IsValid Then
                Throw New Exception(String.Format(My.Resources.Common_ContainsErrors, vbCrLf, _
                    GetAllBrokenRules()))
            End If

            Return MyBase.Save

        End Function


        Private Sub DebetList_Changed(ByVal sender As Object, _
            ByVal e As System.ComponentModel.ListChangedEventArgs) Handles _DebetList.ListChanged
            _DebetSum = _DebetList.GetSum
            PropertyHasChanged("DebetSum")
        End Sub

        Private Sub CreditList_Changed(ByVal sender As Object, _
            ByVal e As System.ComponentModel.ListChangedEventArgs) Handles _CreditList.ListChanged
            _CreditSum = _CreditList.GetSum
            PropertyHasChanged("CreditSum")
        End Sub

        ''' <summary>
        ''' Helper method. Takes care of child lists loosing their handlers.
        ''' </summary>
        Protected Overrides Function GetClone() As Object
            Dim result As JournalEntry = DirectCast(MyBase.GetClone(), JournalEntry)
            result.RestoreChildListsHandles()
            Return result
        End Function

        Protected Overrides Sub OnDeserialized(ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.OnDeserialized(context)
            RestoreChildListsHandles()
        End Sub

        Protected Overrides Sub UndoChangesComplete()
            MyBase.UndoChangesComplete()
            RestoreChildListsHandles()
        End Sub

        ''' <summary>
        ''' Helper method. Takes care of DebetList and CreditList loosing their handlers. See GetClone method.
        ''' </summary>
        Friend Sub RestoreChildListsHandles()

            Try
                RemoveHandler _DebetList.ListChanged, AddressOf DebetList_Changed
                RemoveHandler _CreditList.ListChanged, AddressOf CreditList_Changed
            Catch ex As Exception
            End Try
            AddHandler _DebetList.ListChanged, AddressOf DebetList_Changed
            AddHandler _CreditList.ListChanged, AddressOf CreditList_Changed

        End Sub


        ''' <summary>
        ''' Gets a list of debet and credit book entries formated as e.g. "D 2711, K 443". 
        ''' </summary>
        Public Function GetCorrespondentionsString() As String

            Dim resultList As New List(Of String)
            resultList.Add(_DebetList.GetEntryListString)
            resultList.Add(_CreditList.GetEntryListString)

            Dim result As String = String.Join(", ", resultList.ToArray)

            If result.Trim.Length > 254 Then result = result.Trim.Substring(0, 249) & "<...>"

            Return result

        End Function

        ''' <summary>
        ''' Loads JournalEntryTemplate data (content and corespondences) to JournalEntry.
        ''' Clears current corespondences. In case of an old JournalEntry 
        ''' cleared corespondences are moved to DeletedList.
        ''' </summary>
        ''' <param name="EntryTemplate">JournalEntryTemplate object containing the data to load.</param>
        Public Sub LoadJournalEntryFromTemplate(ByVal entryTemplate As HelperLists.TemplateJournalEntryInfo)

            Content = entryTemplate.Content
            _DebetList.LoadBookEntryListFromTemplate(entryTemplate.DebetList, True)
            _CreditList.LoadBookEntryListFromTemplate(entryTemplate.CreditList, True)

        End Sub

        ''' <summary>
        ''' Adds the difference betweeen DebetSum and CreditSum to the debet or credit entry
        ''' without an amount specified.
        ''' </summary>
        Public Sub BalanceEntriesAmounts()
            If CRound(_DebetSum) = CRound(_CreditSum) Then
                Throw New Exception(General_JournalEntry_CannotBalanceAlreadyBalanced)
            End If
            If CRound(_DebetSum) > CRound(_CreditSum) Then
                For Each entry As BookEntry In _CreditList
                    If Not entry.Amount > 0 Then
                        entry.Amount = CRound(_DebetSum - _CreditSum)
                        Exit Sub
                    End If
                Next
                Throw New Exception(General_JournalEntry_CannotBalanceCredit)
            Else
                For Each entry As BookEntry In _DebetList
                    If Not entry.Amount > 0 Then
                        entry.Amount = CRound(_CreditSum - _DebetSum)
                        Exit Sub
                    End If
                Next
                Throw New Exception(General_JournalEntry_CannotBalanceDebit)
            End If
        End Sub


        ''' <summary>
        ''' Gets a (financial) copy of the JournalEntry as a new JournalEntry instance.
        ''' </summary>
        ''' <remarks></remarks>
        Public Function GetJournalEntryCopy() As JournalEntry

            Dim result As New JournalEntry

            result._ChronologyValidator = SimpleChronologicValidator.NewSimpleChronologicValidator( _
                My.Resources.General_JournalEntry_TypeName, Nothing)
            result._Content = _Content
            result._Date = _Date
            result._DocNumber = _DocNumber
            result._DocTypeHumanReadable = Utilities.ConvertLocalizedName(result._DocType)
            result._Person = _Person

            result._DebetList = BookEntryList.NewBookEntryList(BookEntryType.Debetas)
            result._CreditList = BookEntryList.NewBookEntryList(BookEntryType.Kreditas)

            For Each entry As BookEntry In _CreditList
                result._CreditList.Add(entry.GetBookEntryCopy())
            Next
            For Each entry As BookEntry In _DebetList
                result._DebetList.Add(entry.GetBookEntryCopy())
            Next

            result._CreditSum = _CreditSum
            result._DebetSum = _DebetSum

            result.ValidationRules.CheckRules()

            Return result

        End Function


        Public Function GetAllBrokenRules() As String _
            Implements IValidationMessageProvider.GetAllBrokenRules
            Dim result As String = ""
            If Not MyBase.IsValid Then result = AddWithNewLine(result, _
                Me.BrokenRulesCollection.ToString(Validation.RuleSeverity.Error), False)
            If Not _DebetList.IsValid Then result = AddWithNewLine(result, _
                _DebetList.GetAllBrokenRules, False)
            If Not _CreditList.IsValid Then result = AddWithNewLine(result, _
                _CreditList.GetAllBrokenRules, False)
            Return result
        End Function

        Public Function GetAllWarnings() As String _
            Implements IValidationMessageProvider.GetAllWarnings
            Dim result As String = ""
            If Me.BrokenRulesCollection.WarningCount > 0 Then
                result = AddWithNewLine(result, Me.BrokenRulesCollection.ToString(Validation.RuleSeverity.Warning), False)
            End If
            If _DebetList.HasWarnings Then
                result = AddWithNewLine(result, _DebetList.GetAllWarnings, False)
            End If
            If _CreditList.HasWarnings Then
                result = AddWithNewLine(result, _CreditList.GetAllWarnings, False)
            End If
            Return result
        End Function

        Public Function HasWarnings() As Boolean _
            Implements IValidationMessageProvider.HasWarnings
            Return Me.BrokenRulesCollection.WarningCount > 0 OrElse _DebetList.HasWarnings _
                OrElse _CreditList.HasWarnings
        End Function


        Protected Overrides Function GetIdValue() As Object
            If IsNew Then
                Return _Guid.ToString()
            Else
                Return _ID.ToString
            End If
        End Function

        Public Overrides Function ToString() As String
            Return String.Format(My.Resources.General_JournalEntry_ToString, _
                IIf(IsNew, My.Resources.Common_NewObject, My.Resources.Common_ExistingObject), _
                _ID.ToString, _Date.ToShortDateString, _DocNumber, _Content, _DebetList.ToString(), _
                _CreditList.ToString())
        End Function

#End Region

#Region " Validation Rules "

        Protected Overrides Sub AddBusinessRules()

            ValidationRules.AddRule(AddressOf CommonValidation.ChronologyValidation, _
                New CommonValidation.ChronologyRuleArgs("Date", "ChronologyValidator"))
            ValidationRules.AddRule(AddressOf CommonValidation.StringFieldValidation, _
                New Csla.Validation.RuleArgs("DocNumber"))
            ValidationRules.AddRule(AddressOf CommonValidation.StringFieldValidation, _
                New Csla.Validation.RuleArgs("Content"))
            ValidationRules.AddRule(AddressOf CommonValidation.DoubleFieldValidation, _
                New Csla.Validation.RuleArgs("DebetSum"))
            ValidationRules.AddRule(AddressOf CommonValidation.DoubleFieldValidation, _
                New Csla.Validation.RuleArgs("CreditSum"))
            ValidationRules.AddRule(AddressOf CommonValidation.PersonFieldValidation, _
                New Csla.Validation.RuleArgs("Person"))

            ValidationRules.AddRule(AddressOf DebetEqualsCreditValidation, New Validation.RuleArgs("DebetSum"))

            ValidationRules.AddDependantProperty("CreditSum", "DebetSum", False)

        End Sub

        ''' <summary>
        ''' Rule ensuring that Debet BookEntries.GetSum = Credit BookEntries.GetSum.
        ''' </summary>
        ''' <param name="target">Object containing the data to validate</param>
        ''' <param name="e">Arguments parameter specifying the name of the string
        ''' property to validate</param>
        ''' <returns><see langword="false" /> if the rule is broken</returns>
        <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")> _
        Private Shared Function DebetEqualsCreditValidation(ByVal target As Object, _
            ByVal e As Validation.RuleArgs) As Boolean

            Dim ValObj As JournalEntry = DirectCast(target, JournalEntry)

            If Not CRound(ValObj._DebetSum) > 0 OrElse Not CRound(ValObj._CreditSum) > 0 Then Return True

            If CRound(ValObj._DebetSum) <> CRound(ValObj._CreditSum) Then
                e.Description = String.Format(General_JournalEntry_DebitNotEqualsCredit,
                    CRound(Math.Abs(ValObj._DebetSum - ValObj._CreditSum)).ToString("##,#.00"))
                e.Severity = Validation.RuleSeverity.Error
                Return False
            End If

            Return True

        End Function

#End Region

#Region " Authorization Rules "

        Protected Overrides Sub AddAuthorizationRules()
            AuthorizationRules.AllowWrite("General.JournalEntry2")
        End Sub

        Public Shared Function CanGetObject() As Boolean
            Return ApplicationContext.User.IsInRole("General.JournalEntry1")
        End Function

        Public Shared Function CanAddObject() As Boolean
            Return ApplicationContext.User.IsInRole("General.JournalEntry2")
        End Function

        Public Shared Function CanEditObject() As Boolean
            Return ApplicationContext.User.IsInRole("General.JournalEntry3")
        End Function

        Public Shared Function CanDeleteObject() As Boolean
            Return ApplicationContext.User.IsInRole("General.JournalEntry3")
        End Function

#End Region

#Region " Factory Methods "

        ''' <summary>
        ''' Gets as new JournalEntry as a standalone business object.
        ''' </summary>
        Public Shared Function NewJournalEntry() As JournalEntry
            If Not CanAddObject() Then Throw New System.Security.SecurityException( _
                My.Resources.Common_SecurityInsertDenied)
            Return New JournalEntry(DocumentType.None)
        End Function

        ''' <summary>
        ''' Gets as new JournalEntry as a part of another business object.
        ''' </summary>
        ''' <param name="associatedDocumentType"><see cref="DocumentType">Type</see> of parent business object.</param>
        ''' <param name="pluginCode">a type code of the external plugin document (only applicable
        ''' if the associatedDocumentType equals <see cref="DocumentType.Custom">DocumentType.Custom</see>)</param>
        ''' <param name="allowGenericJournalEntry">whether to allow creating of a 
        ''' <see cref="DocumentType.None">generic journal entry</see></param>
        ''' <remarks>Should only be called on server side.</remarks>
        Friend Shared Function NewJournalEntryChild(ByVal associatedDocumentType As DocumentType, _
            Optional ByVal pluginCode As String = "", Optional ByVal allowGenericJournalEntry As Boolean = False) As JournalEntry
            If Not allowGenericJournalEntry AndAlso associatedDocumentType = DocumentType.None Then
                Throw New InvalidOperationException(My.Resources.General_JournalEntry_UnexpectedChildType)
            End If
            Return New JournalEntry(associatedDocumentType, pluginCode)
        End Function

        ''' <summary>
        ''' Gets an existing JournalEntry as a standalone business object.
        ''' </summary>
        ''' <param name="journalEntryID"><see cref="ID">ID</see> of the JournalEntry.</param>
        Public Shared Function GetJournalEntry(ByVal journalEntryID As Integer) As JournalEntry
            Return DataPortal.Fetch(Of JournalEntry)(New Criteria(journalEntryID))
        End Function

        ''' <summary>
        ''' Gets an existing JournalEntry as a part of another business object.
        ''' </summary>
        ''' <param name="journalEntryID">an <see cref="ID">ID</see> of the JournalEntry</param>
        ''' <param name="ExpectedType">a <see cref="DocumentType">type</see> of parent business object
        ''' that the journal entry is fetched for</param>
        ''' <param name="expectedPluginCode">a type code of the (external plugin) parent business object
        ''' that the journal entry is fetched for (only applicable if the associatedDocumentType equals 
        ''' <see cref="DocumentType.Custom">DocumentType.Custom</see>)</param>
        ''' <param name="allowGenericJournalEntry">whether to allow fetching of a 
        ''' <see cref="DocumentType.None">generic journal entry</see></param>
        ''' <remarks>Should only be called on server side.</remarks>
        Friend Shared Function GetJournalEntryChild(ByVal journalEntryID As Integer, _
            ByVal expectedType As DocumentType, Optional ByVal expectedPluginCode As String = "", _
            Optional ByVal allowGenericJournalEntry As Boolean = False) As JournalEntry

            If Not allowGenericJournalEntry AndAlso expectedType = DocumentType.None Then
                Throw New InvalidOperationException(My.Resources.General_JournalEntry_UnexpectedChildType)
            End If

            Dim result As JournalEntry = New JournalEntry(journalEntryID)

            If result._DocType <> expectedType Then Throw New InvalidOperationException( _
                String.Format(My.Resources.General_JournalEntry_UnexpectedParentType, _
                journalEntryID.ToString, result._DocTypeHumanReadable, _
                Utilities.ConvertLocalizedName(expectedType)))

            Return result

        End Function

        ''' <summary>
        ''' Deletes an existing JournalEntry from database.
        ''' </summary>
        ''' <param name="journalEntryID"><see cref="ID">ID</see> of the JournalEntry to delete.</param>
        Public Shared Sub DeleteJournalEntry(ByVal journalEntryID As Integer)
            DataPortal.Delete(New Criteria(journalEntryID))
        End Sub

        ''' <summary>
        ''' Deletes an existing JournalEntry from database.
        ''' </summary>
        ''' <param name="journalEntryID"><see cref="ID">ID</see> of the JournalEntry to delete.</param>
        Friend Shared Sub DeleteJournalEntryChild(ByVal journalEntryID As Integer)
            DoDelete(journalEntryID)
        End Sub


        Private Sub New()
            ' require use of factory methods
        End Sub

        Private Sub New(ByVal associatedDocumentType As DocumentType)
            Create(associatedDocumentType, "")
        End Sub

        Private Sub New(ByVal associatedDocumentType As DocumentType, ByVal pluginCode As String)
            MarkAsChild()
            Create(associatedDocumentType, pluginCode)
        End Sub

        Private Sub New(ByVal journalEntryID As Integer)
            MarkAsChild()
            DoFetch(journalEntryID)
        End Sub

#End Region

#Region " Data Access "

        <Serializable()> _
        Private Class Criteria
            Private _ID As Integer
            Public ReadOnly Property ID() As Integer
                Get
                    Return _ID
                End Get
            End Property
            Public Sub New(ByVal nID As Integer)
                _ID = nID
            End Sub
        End Class


        Private Sub Create(ByVal associatedDocumentType As DocumentType, ByVal pluginCode As String)

            If _DocType <> DocumentType.Custom Then
                pluginCode = ""
            ElseIf StringIsNullOrEmpty(pluginCode) Then
                Throw New Exception(General_JournalEntry_PluginDocumentCodeNull)
            End If

            _DocType = associatedDocumentType
            _DocTypeHumanReadable = Utilities.ConvertLocalizedName(associatedDocumentType)
            _PluginDocTypeCode = pluginCode
            If _DocType = DocumentType.Custom Then
                _DocTypeHumanReadable = String.Format("{0}\{1}", _DocTypeHumanReadable, _
                    AccPluginManager.PluginManager.Current.GetLocalizedDocumentType(pluginCode))
            End If
            _DebetList = BookEntryList.NewBookEntryList(BookEntryType.Debetas)
            _CreditList = BookEntryList.NewBookEntryList(BookEntryType.Kreditas)
            _ChronologyValidator = SimpleChronologicValidator.NewSimpleChronologicValidator( _
                My.Resources.General_JournalEntry_TypeName, Nothing)

            ValidationRules.CheckRules()

        End Sub


        Private Overloads Sub DataPortal_Fetch(ByVal criteria As Criteria)
            If Not CanGetObject() Then Throw New System.Security.SecurityException( _
                My.Resources.Common_SecuritySelectDenied)
            DoFetch(criteria.ID)
            If _DocType <> DocumentType.None Then MarkAsChild()
        End Sub

        Private Sub DoFetch(ByVal journalEntryID As Integer)

            Dim myComm As New SQLCommand("JournalEntryFetch")
            myComm.AddParam("?BD", journalEntryID)

            Using myData As DataTable = myComm.Fetch

                If Not myData.Rows.Count > 0 Then
                    Throw New Exception(String.Format(My.Resources.Common_ObjectNotFound, _
                        My.Resources.General_JournalEntry_TypeName, journalEntryID.ToString))
                End If

                Dim dr As DataRow = myData.Rows(0)

                _ID = journalEntryID
                _Date = CDateSafe(dr.Item(0), Today)
                _DocNumber = CStrSafe(dr.Item(1)).Trim
                _Content = CStrSafe(dr.Item(2)).Trim
                _DocType = Utilities.ConvertDatabaseCharID(Of DocumentType)(CStrSafe(dr.Item(3)).Trim)
                _DocTypeHumanReadable = Utilities.ConvertLocalizedName(_DocType)
                _PluginDocTypeCode = CStrSafe(dr.Item(4)).Trim
                If _DocType = DocumentType.None AndAlso Not StringIsNullOrEmpty(_PluginDocTypeCode) Then
                    _DocTypeHumanReadable = String.Format("{0}\{1}", _DocTypeHumanReadable, _
                        AccPluginManager.PluginManager.Current.GetLocalizedDocumentType(_PluginDocTypeCode))
                End If
                _InsertDate = CTimeStampSafe(dr.Item(5))
                _UpdateDate = CTimeStampSafe(dr.Item(6))
                _Person = HelperLists.PersonInfo.GetPersonInfo(dr, 7)

            End Using

            _ChronologyValidator = SimpleChronologicValidator.GetSimpleChronologicValidator(_ID, Nothing)

            myComm = New SQLCommand("BookEntriesFetch")
            myComm.AddParam("?BD", journalEntryID)

            Using myData As DataTable = myComm.Fetch
                _DebetList = BookEntryList.GetBookEntryList(myData, BookEntryType.Debetas, _
                    _ChronologyValidator.FinancialDataCanChange, _
                    _ChronologyValidator.FinancialDataCanChangeExplanation)
                _CreditList = BookEntryList.GetBookEntryList(myData, BookEntryType.Kreditas, _
                    _ChronologyValidator.FinancialDataCanChange, _
                    _ChronologyValidator.FinancialDataCanChangeExplanation)
            End Using

            _DebetSum = _DebetList.GetSum
            _CreditSum = _CreditList.GetSum

            MarkOld()

            ValidationRules.CheckRules()

        End Sub


        ''' <summary>
        ''' Saves the object as a child of some other object bypassing DataPortal and returns the saved instance.
        ''' </summary>
        ''' <remarks>Should only be invoked on server side.
        ''' Should only be invoked on child objects that were created or fetched using child factory methods.</remarks>
        Friend Function SaveChild() As JournalEntry

            If Not IsChild Then Throw New Exception(My.Resources.Common_InvalidSaveChild)

            If _DocType = DocumentType.None Then
                Throw New InvalidOperationException(My.Resources.General_JournalEntry_UnexpectedChildType)
            End If

            If Not Me.IsValid Then
                Throw New InvalidOperationException(String.Format(My.Resources.Common_ErrorInItem, _
                    Me.ToString, vbCrLf, GetAllBrokenRules()))
            End If

            Dim result As JournalEntry = Me.Clone

            If result.IsNew Then
                result.DoInsert()
            Else
                result.DoUpdate()
            End If

            Return result

        End Function

        Protected Overrides Sub DataPortal_Insert()

            If Not CanAddObject() Then Throw New System.Security.SecurityException( _
                My.Resources.Common_SecurityInsertDenied)

            If _DocType <> DocumentType.None Then Throw New InvalidOperationException( _
                My.Resources.General_JournalEntry_InvalidTypeOnSave)

            Me.ValidationRules.CheckRules()
            If Not Me.IsValid Then
                Throw New Exception(String.Format(My.Resources.Common_ContainsErrors, vbCrLf, _
                    GetAllBrokenRules()))
            End If

            DoInsert()

        End Sub

        Private Sub DoInsert()

            Using transaction As New SqlTransaction

                Try

                    Dim myComm As New SQLCommand("JournalEntryInsert")
                    AddWithParams(myComm)
                    myComm.AddParam("?AG", _PluginDocTypeCode.Trim)
                    myComm.AddParam("?AD", Utilities.ConvertDatabaseCharID(_DocType))

                    myComm.Execute()

                    _ID = Convert.ToInt32(myComm.LastInsertID)

                    _DebetList.Update(Me)
                    _CreditList.Update(Me)

                    transaction.Commit()

                Catch ex As Exception
                    transaction.SetNonSqlException(ex)
                    Throw
                End Try

            End Using

            MarkOld()

        End Sub

        Protected Overrides Sub DataPortal_Update()

            If Not CanEditObject() Then Throw New System.Security.SecurityException( _
                My.Resources.Common_SecurityUpdateDenied)

            If _DocType <> DocumentType.None Then Throw New InvalidOperationException( _
                My.Resources.General_JournalEntry_InvalidTypeOnSave)

            Me.ValidationRules.CheckRules()
            If Not Me.IsValid Then
                Throw New Exception(String.Format(My.Resources.Common_ContainsErrors, vbCrLf, _
                    GetAllBrokenRules()))
            End If

            CheckIfUpdateDateChanged()

            DoUpdate()

        End Sub

        Private Sub DoUpdate()

            Using transaction As New SqlTransaction

                Try

                    Dim myComm As New SQLCommand("JournalEntryUpdate")
                    AddWithParams(myComm)
                    myComm.AddParam("?BD", _ID)

                    myComm.Execute()

                    If _ChronologyValidator.FinancialDataCanChange Then
                        _DebetList.Update(Me)
                        _CreditList.Update(Me)
                    End If

                    transaction.Commit()

                Catch ex As Exception
                    transaction.SetNonSqlException(ex)
                    Throw
                End Try

            End Using

            MarkOld()

        End Sub


        Protected Overrides Sub DataPortal_DeleteSelf()
            DataPortal_Delete(New Criteria(_ID))
        End Sub

        Protected Overrides Sub DataPortal_Delete(ByVal criteria As Object)

            If Not CanDeleteObject() Then Throw New System.Security.SecurityException( _
                My.Resources.Common_SecurityUpdateDenied)

            Dim indirectRelations As IndirectRelationInfoList = IndirectRelationInfoList. _
                GetIndirectRelationInfoListChild(DirectCast(criteria, Criteria).ID)

            indirectRelations.CheckIfJournalEntryCanBeDeleted(New DocumentType() _
                {DocumentType.None, DocumentType.ClosingEntries})

            CheckIfDirectDeletionIsPossible(indirectRelations.DocType, True)

            DoDelete(DirectCast(criteria, Criteria).ID)

            ' Last closing date is part of CompanyInfo object in GlobalContext
            If indirectRelations.DocType = DocumentType.ClosingEntries Then _
                ApskaitaObjects.Settings.CompanyInfo.LoadCompanyInfoToGlobalContext("", "")

        End Sub

        Private Shared Sub DoDelete(ByVal JournalEntryID As Integer)

            Using transaction As New SqlTransaction

                Try

                    Dim myComm As New SQLCommand("JournalEntryDelete")
                    myComm.AddParam("?BD", JournalEntryID)
                    myComm.Execute()

                    Dim myComm2 As New SQLCommand("BookEntryClear")
                    myComm2.AddParam("?BD", JournalEntryID)
                    myComm2.Execute()

                    transaction.Commit()

                Catch ex As Exception
                    transaction.SetNonSqlException(ex)
                    Throw
                End Try

            End Using

        End Sub


        Private Sub AddWithParams(ByRef myComm As SQLCommand)

            myComm.AddParam("?AA", _Date.Date)
            myComm.AddParam("?AB", _DocNumber.Trim)
            myComm.AddParam("?AC", _Content.Trim)

            If Not _Person Is Nothing AndAlso _Person.ID > 0 Then
                myComm.AddParam("?AE", _Person.ID)
            Else
                myComm.AddParam("?AE", 0)
            End If
            myComm.AddParam("?AF", Me.GetCorrespondentionsString)

            _UpdateDate = GetCurrentTimeStamp()
            If Me.IsNew Then _InsertDate = _UpdateDate

            myComm.AddParam("?AH", _UpdateDate.ToUniversalTime)

        End Sub

        Private Sub CheckIfUpdateDateChanged()

            Dim myComm As New SQLCommand("CheckIfJournalEntryUpdateDateChanged")
            myComm.AddParam("?CD", _ID)

            Using myData As DataTable = myComm.Fetch

                If myData.Rows.Count < 1 OrElse CDateTimeSafe(myData.Rows(0).Item(0), _
                    Date.MinValue) = Date.MinValue Then

                    Throw New Exception(String.Format(My.Resources.Common_ObjectNotFound, _
                        My.Resources.General_JournalEntry_TypeName, _ID.ToString))

                End If

                If CTimeStampSafe(myData.Rows(0).Item(0)) <> _UpdateDate Then

                    Throw New Exception(My.Resources.Common_UpdateDateHasChanged)

                End If

            End Using

        End Sub

        ''' <summary>
        ''' Indicates if the JournalEntry can be deleted as standalone document, 
        ''' i.e. is not a part of some complex document, e.g. invoice. 
        ''' </summary>
        Private Shared Function CheckIfDirectDeletionIsPossible(ByVal associatedDocumentType As DocumentType, _
            ByVal throwOnNotPossible As Boolean) As Boolean

            ' allowed types
            If associatedDocumentType = DocumentType.ClosingEntries _
                OrElse associatedDocumentType = DocumentType.None _
                OrElse associatedDocumentType = DocumentType.TransferOfBalance Then
                Return True
            End If

            ' disallowed types
            Dim ExceptionMessage As String = ""

            If associatedDocumentType = DocumentType.AdvanceReport OrElse _
                associatedDocumentType = DocumentType.InvoiceMade OrElse _
                associatedDocumentType = DocumentType.InvoiceReceived OrElse _
                associatedDocumentType = DocumentType.AccumulatedCosts OrElse _
                associatedDocumentType = DocumentType.BankOperation OrElse _
                associatedDocumentType = DocumentType.Offset OrElse _
                associatedDocumentType = DocumentType.TillIncomeOrder OrElse _
                associatedDocumentType = DocumentType.TillSpendingOrder Then

                ExceptionMessage = String.Format(My.Resources.General_JournalEntry_OnDeletingDocumentModule, _
                    Utilities.ConvertLocalizedName(associatedDocumentType))

            ElseIf associatedDocumentType = DocumentType.Amortization OrElse _
                associatedDocumentType = DocumentType.LongTermAssetAccountChange OrElse _
                associatedDocumentType = DocumentType.LongTermAssetDiscard Then

                ExceptionMessage = String.Format(My.Resources.General_JournalEntry_OnDeletingAssetsModule, _
                    Utilities.ConvertLocalizedName(associatedDocumentType))

            ElseIf associatedDocumentType = DocumentType.GoodsAccountChange OrElse _
                associatedDocumentType = DocumentType.GoodsInternalTransfer OrElse _
                associatedDocumentType = DocumentType.GoodsInventorization OrElse _
                associatedDocumentType = DocumentType.GoodsProduction OrElse _
                associatedDocumentType = DocumentType.GoodsRevalue OrElse _
                associatedDocumentType = DocumentType.GoodsWriteOff Then

                ExceptionMessage = String.Format(My.Resources.General_JournalEntry_OnDeletingGoodsModule, _
                    Utilities.ConvertLocalizedName(associatedDocumentType))

            ElseIf associatedDocumentType = DocumentType.ImprestSheet OrElse _
                associatedDocumentType = DocumentType.WageSheet Then

                ExceptionMessage = String.Format(My.Resources.General_JournalEntry_OnDeletingWorkersModule, _
                    Utilities.ConvertLocalizedName(associatedDocumentType))

            Else

                Throw New NotImplementedException(String.Format(My.Resources.Common_DocumentTypeNotImplemented, _
                    associatedDocumentType.ToString, GetType(JournalEntry).FullName, "DirectDeletionIsPossible"))

            End If

            If throwOnNotPossible Then Throw New Exception(ExceptionMessage)

            Return False

        End Function

#End Region

    End Class

End Namespace