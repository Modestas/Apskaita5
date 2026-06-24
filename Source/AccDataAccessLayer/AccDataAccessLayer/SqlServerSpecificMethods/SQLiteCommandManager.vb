Imports System.Data.SQLite
Imports AccDataAccessLayer.DatabaseAccess
Imports System.Configuration
Namespace SqlServerSpecificMethods

    Friend Class SQLiteCommandManager
        Implements ISqlCommandManager

        ' All paths are relative to:
        ' In case of winforms - to program instalation folder.
        ' In case of webservice - to App_Data. (see Helpers.Utilities.AppPath)     Pooling=True;Max Pool Size=100;
        Friend Const Path_SQLiteDepositoryFile As String = "\SqlDepositories\SQLite_Depository.sqld"
        Private Const ConnString_WithoutPassword As String = "Data Source={0};Version=3;UseUTF16Encoding=True;Pooling=True;Max Pool Size=100;Cache Size=500000;"
        Private Const ConnString As String = "Data Source={0};Version=3;UseUTF16Encoding=True;Cache Size=500000;Password={1}"


        Public Sub TransactionBegin(Optional ByVal ThrowOnTransactionExists As Boolean = True) _
            Implements ISqlCommandManager.TransactionBegin

            Dim Conn As SQLiteConnection = OpenConnection()
            Dim myTrans As SQLiteTransaction = Conn.BeginTransaction()
            Dim myComm As New SQLiteCommand()
            myComm.Connection = Conn
            myComm.CommandTimeout = GetSqlQueryTimeOut()
            myComm.Transaction = myTrans

            Csla.ApplicationContext.LocalContext.Add(LC_TransactionObjectKey, myComm)
            Csla.ApplicationContext.LocalContext.Add(LC_TransactionStatementListKey, New List(Of String))

        End Sub

        Public Sub TransactionCommit() _
            Implements ISqlCommandManager.TransactionCommit

            Dim myTrans As SQLiteTransaction = TransactionGetObject()

            Try
                myTrans.Commit()
            Catch ex As Exception
                Try
                    TransactionRollBack(ex)
                Catch e As Exception
                    TransactionEnd()
                    Throw
                End Try
            End Try

            TransactionEnd()

        End Sub

        Public Function ExecuteCommand(ByVal CommandToExecute As DatabaseAccess.SQLCommand, _
            Optional ByRef RowsAffected As Integer = 0) As Long _
            Implements ISqlCommandManager.ExecuteCommand

            Dim Conn As SQLiteConnection = Nothing

            Dim myCommand As SQLiteCommand = GetCommand(Conn, CommandToExecute)

            Dim result As Long = -1
            myCommand.CommandText = myCommand.CommandText & " SELECT last_insert_rowid() AS [ID];"

            Try
                result = CInt(myCommand.ExecuteScalar)
                RowsAffected = -1 ' its impossible to get both lastinsertid and rows affected
            Catch ex As Exception
                If TransactionExists() Then
                    TransactionRollBack(New Exception("Klaida vykdant SQL komandą: " _
                        & CommandToExecute.ToString & vbCrLf & "Klaidos turinys: '" & _
                        ex.Message & "'.", ex))
                Else
                    Try
                        If Conn.State = ConnectionState.Open Then Conn.Close()
                        Conn.Dispose()
                    Catch e As Exception
                    End Try
                    Throw New Exception("Klaida vykdant SQL komandą: " & CommandToExecute.ToString _
                        & vbCrLf & "Klaidos turinys: '" & ex.Message & "'.", ex)
                End If
            End Try

            If Not TransactionExists() Then
                Try
                    If Conn.State = ConnectionState.Open Then Conn.Close()
                    Conn.Dispose()
                    myCommand.Dispose()
                Catch e As Exception
                End Try
            End If

            Return result

        End Function

        Public Function FetchCommand(ByVal SelectCommand As DatabaseAccess.SQLCommand) _
            As System.Data.DataTable Implements ISqlCommandManager.FetchCommand

            Dim myError As Exception = Nothing
            Dim result As DataTable = Nothing

            Dim Conn As SQLiteConnection = Nothing

            Dim myCommand As SQLiteCommand = GetCommand(Conn, SelectCommand)

            Try

                Using myAdapter As New SQLiteDataAdapter

                    myAdapter.SelectCommand = myCommand

                    result = New DataTable

                    myAdapter.Fill(result)
                    
                End Using

            Catch ex As Exception

                If Not result Is Nothing Then result.Dispose()

                If TransactionExists() Then

                    TransactionRollBack(New Exception("Klaida vykdant SQL komandą: " _
                        & SelectCommand.ToString & vbCrLf & "Klaidos turinys: '" & _
                        ex.Message & "'.", ex))

                Else

                    Try
                        If Conn.State = ConnectionState.Open Then Conn.Close()
                        Conn.Dispose()
                    Catch e As Exception
                    End Try
                    Try
                        myCommand.Dispose()
                    Catch exd As Exception
                    End Try

                    Throw New Exception("Klaida vykdant SELECT sakinį (statement): " & _
                        SelectCommand.ToString & vbCrLf & "Klaidos turinys: '" & _
                        ex.Message & "'.", ex)

                End If

            End Try

            If Not TransactionExists() Then

                Try
                    If Conn.State = ConnectionState.Open Then Conn.Close()
                    Conn.Dispose()
                Catch e As Exception
                End Try
                Try
                    myCommand.Dispose()
                Catch exd As Exception
                End Try

            End If

            Return result

        End Function

        Public Function DateTimeToDbString(ByVal nDate As Date) As String _
            Implements ISqlCommandManager.DateTimeToDbString
            Return nDate.ToString("yyyy'-'MM'-'dd HH':'mm':'ss")
        End Function

        Public Function DateToDbString(ByVal nDate As Date) As String _
            Implements ISqlCommandManager.DateToDbString
            Return nDate.ToString("yyyy'-'MM'-'dd")
        End Function

        Public Function TimeToDbString(ByVal nDate As Date) As String _
            Implements ISqlCommandManager.TimeToDbString
            Return nDate.ToString("HH':'mm':'ss")
        End Function

        Public Sub TransactionRollBack(ByVal e As System.Exception) _
            Implements ISqlCommandManager.TransactionRollBack

            Dim tmpList As List(Of String) = GetExecutedTransactionStatementsList()

            Dim myError As New Exception("Klaida transakcijoje vykdant SQL sakinį " & _
                tmpList.Item(tmpList.Count - 1) & vbCrLf & "Klaidos turinys: '" & _
                e.Message & "'.", e)

            Dim myTrans As SQLiteTransaction = TransactionGetObject()

            Try
                myTrans.Rollback()
            Catch ex As SQLiteException
                If Not myTrans.Connection Is Nothing Then
                    ' Jei nepavyko rollback reiskiasi labai labai negerai
                    ' Formuojam klaidos loga
                    Dim Tmp As String = vbCrLf & "KRITINĖ DUOMENŲ BAZĖS KLAIDA. Nepavyko atkurti " _
                            & "pradinių duomenų bazės įrašų. Siūlome šios žinutės pilną tekstą " _
                            & "perduoti programuotojui."
                    If tmpList.Count > 1 Then
                        Tmp = Tmp & vbCrLf & "ĮVYKDYTI SAKINIAI: " & vbCrLf
                    Else
                        Tmp = Tmp & vbCrLf & "Jokie sakiniai nebuvo (be klaidos) įvykdyti."
                    End If
                    For i As Integer = 1 To tmpList.Count - 1
                        Tmp = Tmp & vbCrLf & tmpList.Item(i - 1)
                    Next
                    Tmp = Tmp & vbCrLf & "Kritinės klaidos turinys: '" & ex.Message & "'."
                    Tmp = myError.Message & vbCrLf & Tmp
                    myError = New Exception(Tmp, e)
                End If
            End Try

            TransactionEnd()

            Throw myError

        End Sub

        Public Sub FetchCompanyFromDbFile(ByVal DbFilePath As String, _
            ByVal cPassword As String, ByRef cCompanyName As String, _
            ByRef cCompanyCode As String) _
            Implements ISqlCommandManager.FetchCompanyFromDbFile

            Using conn As New SQLiteConnection(String.Format(ConnString, DbFilePath, cPassword))

                Try
                    conn.Open()
                Catch ex As Exception

                    If Not conn Is Nothing AndAlso Not conn.State = ConnectionState.Closed Then conn.Close()

                    If TypeOf ex Is SQLite.SQLiteException AndAlso
                        (CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.Auth OrElse
                        CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.Perm) Then

                        Throw New Exception("Klaida. Neteisingas slaptažodis arba DB " _
                            & "failas yra užrakintas sistemos.", ex)

                    ElseIf TypeOf ex Is SQLite.SQLiteException AndAlso
                        CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.Empty Then

                        Throw New Exception("Klaida. Faile nėra saugoma duomenų bazė.", ex)

                    ElseIf TypeOf ex Is SQLite.SQLiteException AndAlso
                        CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.NotFound Then

                        Throw New Exception("Klaida. Nerastas duomenų bazės failas.", ex)

                    ElseIf TypeOf ex Is SQLite.SQLiteException AndAlso
                        (CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.CantOpen OrElse
                        CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.Busy) Then

                        Throw New Exception("Klaida. Neleidžiama prieiga prie duomenų bazės failo. " _
                            & "Failas gali būti užrakintas sistemos (windows) arba kitos programos.", ex)

                    Else

                        Throw New Exception("Klaida. Nepavyko atidaryti duomenų bazės failo: " _
                            & ex.Message, ex)

                    End If

                End Try

                Dim myComm As New SQLCommand("GetCompanyByDatabase")

                Try
                    Using myData As DataTable = FetchUsingConnection(conn, myComm)
                        If myData.Rows.Count < 1 Then Throw New Exception(
                            "Klaida. Nepavyko gauti įmonės duomenų iš failo. " _
                            & "Tikėtina, kad šiame faile nėra saugoma įmonės duomenų bazė.")
                        If IsDBNull(myData.Rows(0).Item(0)) OrElse IsDBNull(myData.Rows(0).Item(1)) Then _
                            Throw New Exception("Klaida. Nepavyko gauti įmonės duomenų iš faile " _
                            & "esančios įmonės lentelės. Tikėtina, kad ši lentelė yra sugadinta.")

                        cCompanyName = myData.Rows(0).Item(0).ToString.Trim
                        cCompanyCode = myData.Rows(0).Item(1).ToString.Trim

                    End Using
                Catch ex As Exception
                    If Not conn Is Nothing AndAlso Not conn.State = ConnectionState.Closed Then conn.Close()
                    If ex.Message.Trim.ToLower.StartsWith("klaida") Then Throw ex
                    Throw New Exception("Klaida. Nepavyko nuskaityti duomenų iš duomenų bazės failo: " _
                        & ex.Message, ex)
                End Try

                If Not conn Is Nothing AndAlso Not conn.State = ConnectionState.Closed Then conn.Close()
            End Using

        End Sub

        Public Sub TryLogin(ByVal CurrentIdentity As Security.AccIdentity, _
            ByRef cRoles As System.Collections.Generic.List(Of String)) _
            Implements ISqlCommandManager.TryLogin

            If cRoles Is Nothing Then
                cRoles = New List(Of String)
            Else
                cRoles.Clear()
            End If

            If CurrentIdentity.IsLocalUser Then
                TryLoginLocal(CurrentIdentity, cRoles)
                Exit Sub
            End If

            Dim OldDatabase As String = CurrentIdentity.Database
            CurrentIdentity.Database = CurrentIdentity.SecurityDatabase

            Using Conn As New SQLiteConnection(CurrentIdentity.ConnString)
                Try
                    Conn.Open()
                Catch ex As Exception

                    CurrentIdentity.Database = OldDatabase

                    If Not Conn Is Nothing AndAlso Conn.State <> ConnectionState.Closed Then Conn.Close()

                    If TypeOf ex Is SQLite.SQLiteException AndAlso _
                        (CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.Auth OrElse _
                        CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.Perm) Then

                        Throw New Exception("Klaida. Neįmanoma prisijunti prie vartotojų duomenų bazės. " _
                            & "Kreipkitės į serverio administratorių.", ex)

                    ElseIf TypeOf ex Is SQLite.SQLiteException AndAlso _
                        (CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.Empty OrElse _
                        CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.NotFound) Then

                        Throw New Exception("Klaida. Nerasta vartotojų duomenų bazė. " _
                            & "Kreipkitės į serverio administratorių.", ex)

                    ElseIf TypeOf ex Is SQLite.SQLiteException AndAlso _
                        (CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.CantOpen OrElse _
                        CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.Busy) Then

                        Throw New Exception("Klaida. Neleidžiama prieiga prie vartotojų duomenų bazės. " _
                            & "Kreipkitės į serverio administratorių.", ex)

                    Else

                        Throw New Exception("Klaida. Nepavyko prisijungti prie duomenų bazės: " _
                            & ex.Message, ex)

                    End If

                End Try

                Try

                    Dim myComm As SQLCommand

                    If String.IsNullOrEmpty(OldDatabase.Trim) Then

                        myComm = New SQLCommand("RawSQL", "SELECT (SELECT COUNT(D.ID) " _
                            & "FROM users D) AS UserCount, (SELECT COUNT(G.ID) FROM users G " _
                            & "WHERE LOWER(TRIM(G.Name))=LOWER(TRIM($NM)) AND G.Password=$PS) AS UserExists, " _
                            & "(SELECT COUNT(R.RoleName) FROM roles AS R, users AS U " _
                            & "WHERE R.UserID=U.ID AND LOWER(TRIM(R.RoleName))=LOWER(TRIM($AR)) AND " _
                            & "LOWER(TRIM(U.Name))=LOWER(TRIM($NM)) AND U.Password=$PS) AS IsAdmin, " _
                            & "(SELECT G.RealName FROM users G WHERE LOWER(TRIM(G.Name))" _
                            & "=LOWER(TRIM($NM)) AND G.Password=$PS) AS UserRealName;")

                    Else

                        myComm = New SQLCommand("RawSQL", "SELECT R.RoleName, R.RoleLevel " _
                            & "FROM roles AS R, users AS U WHERE R.UserID=U.ID AND " _
                            & "(LOWER(TRIM(R.DatabaseName))=LOWER(TRIM($DT)) OR LOWER(TRIM(R.RoleName))=LOWER(TRIM($AR))) " _
                            & "AND LOWER(TRIM(U.Name))=LOWER(TRIM($NM)) AND U.Password=$PS ;")
                        myComm.AddParam("?DT", OldDatabase.Trim.ToLower)

                    End If

                    myComm.AddParam("?NM", CurrentIdentity.Name.Trim.ToLower)
                    myComm.AddParam("?PS", HashPasswordSha256(CurrentIdentity.Password))
                    myComm.AddParam("?AR", Name_AdminRole.Trim.ToLower)

                    Using myData As DataTable = FetchUsingConnection(Conn, myComm)

                        If String.IsNullOrEmpty(OldDatabase.Trim) Then

                            If myData.Rows.Count < 1 OrElse IsDBNull(myData.Rows(0).Item(0)) _
                                OrElse Not Integer.TryParse(myData.Rows(0).Item(0).ToString, _
                                New Integer) OrElse IsDBNull(myData.Rows(0).Item(1)) _
                                OrElse Not Integer.TryParse(myData.Rows(0).Item(1).ToString, _
                                New Integer) OrElse IsDBNull(myData.Rows(0).Item(2)) _
                                OrElse Not Integer.TryParse(myData.Rows(0).Item(2).ToString, _
                                New Integer) Then

                                Throw New Exception("Klaida. Sugadinta " _
                                    & "vartotojų duomenų bazė. Kreipkitės į administratorių.")

                            End If


                            If CInt(myData.Rows(0).Item(0)) < 1 Then

                                AddAdminUser(CurrentIdentity, Conn)

                                cRoles.Add(Name_AdminRole)
                                CurrentIdentity.Database = OldDatabase
                                If Not Conn Is Nothing AndAlso Conn.State <> ConnectionState.Closed Then _
                                    Conn.Close()
                                Exit Sub

                            End If

                            If Convert.ToInt32(myData.Rows(0).Item(1).ToString) < 1 Then Throw New Exception( _
                                "Klaida. Vartotojas neturi teisės prisijungti prie serverio " _
                                & "arba klaidingai nurodytas vartotojo vardas ar slaptažodis.")

                            If CInt(myData.Rows(0).Item(2)) > 0 Then cRoles.Add(Name_AdminRole)

                        Else

                            If myData.Rows.Count < 1 Then Throw New Exception( _
                                "Klaida. Vartotojas neturi teisės prisijungti prie šios įmonės " _
                                & "arba klaidingai nurodytas vartotojo vardas ar slaptažodis.")

                            For Each dr As DataRow In myData.Rows
                                If dr.Item(0).ToString.Trim.ToLower = Name_AdminRole.ToLower.Trim Then
                                    cRoles.Clear()
                                    cRoles.Add(Name_AdminRole)
                                    Exit For
                                Else
                                    cRoles.Add(dr.Item(0).ToString.Trim & dr.Item(1).ToString.Trim)
                                End If
                            Next

                        End If

                    End Using

                Catch ex As Exception
                    If Not Conn Is Nothing AndAlso Conn.State <> ConnectionState.Closed Then Conn.Close()
                    CurrentIdentity.Database = OldDatabase
                    Throw ex
                End Try

                If Not Conn Is Nothing AndAlso Conn.State <> ConnectionState.Closed Then Conn.Close()
                CurrentIdentity.Database = OldDatabase

            End Using

        End Sub

        ''' <summary>
        ''' Check if the connection to the SQLite server is valid.
        ''' If the connection is valid and database is provided, try to get the roles of the user.
        ''' </summary>
        ''' <param name="cIdentity"> Identity of type AccIdentity to be authenticated.</param>
        ''' <param name="cRoles"> List of roles returned if any.</param>
        Private Sub TryLoginLocal(ByVal cIdentity As Security.AccIdentity, _
            ByRef cRoles As List(Of String))

            Using Conn As New SQLiteConnection(cIdentity.ConnString)

                Try
                    Conn.Open()
                Catch ex As Exception

                    If Not Conn Is Nothing AndAlso Conn.State <> ConnectionState.Closed Then Conn.Close()

                    If TypeOf ex Is SQLite.SQLiteException AndAlso
                        (CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.Auth OrElse
                        CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.Perm  OrElse
                         CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.NotADatabase) Then

                        Throw New Exception("Klaida. Jūs neturite teisės prisijungti prie duomenų bazės " _
                            & "arba klaidingai nurodėte slaptažodį.", ex)

                        'ElseIf TypeOf ex Is SQLite.SQLiteException AndAlso
                        '    CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.NotADatabase Then


                    ElseIf TypeOf ex Is SQLite.SQLiteException AndAlso _
                        (CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.Empty OrElse _
                        CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.NotFound) Then

                        Throw New Exception("Klaida. Nerasta duomenų bazė (dingo).", ex)

                    ElseIf TypeOf ex Is SQLite.SQLiteException AndAlso _
                        (CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.CantOpen OrElse _
                        CType(ex, SQLite.SQLiteException).ErrorCode = SQLiteErrorCode.Busy) Then

                        Throw New Exception("Klaida. Neleidžiama prieiga prie duomenų bazės. " _
                            & "Kreipkitės į kompiuterio administratorių.", ex)

                    Else

                        Throw New Exception("Klaida. Nepavyko prisijungti prie duomenų bazės: " _
                            & ex.Message, ex)

                    End If

                End Try

                If Not Conn Is Nothing AndAlso Not Conn.State = ConnectionState.Closed Then Conn.Close()

            End Using

            If cRoles Is Nothing Then
                cRoles = New List(Of String)
            Else
                cRoles.Clear()
            End If
            cRoles.Add(Name_AdminRole)

        End Sub

        Private Function FetchUsingConnection(ByVal Conn As SQLiteConnection, _
            ByVal myComm As SQLCommand) As DataTable

            Dim result As DataTable = New DataTable

            Using myCommand As New SQLiteCommand

                myCommand.Connection = Conn
                myCommand.CommandTimeout = GetSqlQueryTimeOut()

                AddWithSqlCommand(myCommand, myComm)

                Using myAdapter As New SQLiteDataAdapter

                    myAdapter.SelectCommand = myCommand

                    Try
                        myAdapter.Fill(result)
                    Catch ex As Exception
                        If Not Conn Is Nothing AndAlso Conn.State <> ConnectionState.Closed Then Conn.Close()
                        result.Dispose()
                        Throw New Exception("Klaida vykdant SELECT sakinį (statement): " & _
                            myCommand.CommandText & vbCrLf & "Klaidos turinys: '" & ex.Message & "'.", ex)
                    End Try

                End Using

            End Using

            Return result

        End Function

        Private Function ExecuteUsingConnection(ByVal Conn As SQLiteConnection, _
            ByVal myComm As SQLCommand) As Integer

            Dim result As Integer = 0

            Using myCommand As New SQLiteCommand

                myCommand.Connection = Conn
                myCommand.CommandTimeout = GetSqlQueryTimeOut()
                AddWithSqlCommand(myCommand, myComm)
                myCommand.CommandText = myComm.Sentence & " SELECT last_insert_rowid() AS [ID];"

                Try
                    result = myCommand.ExecuteNonQuery
                Catch ex As Exception
                    If Not Conn Is Nothing AndAlso Conn.State <> ConnectionState.Closed Then Conn.Close()
                    Throw New Exception("Klaida vykdant sakinį (statement): " & _
                        myCommand.CommandText & vbCrLf & "Klaidos turinys: '" & ex.Message & "'.", ex)
                End Try

            End Using

            Return result

        End Function

        Private Sub AddAdminUser(ByVal cIdentity As Security.AccIdentity, _
            ByVal Conn As SQLiteConnection)

            If String.IsNullOrEmpty(cIdentity.Password.Trim) Then Throw New Exception( _
                "Klaida. Privalo būti nurodytas slaptažodis.")

            Dim myComm As New SQLCommand("RawSQL", "INSERT INTO users(Name, RealName, Password, " _
                & "UserPosition, UserDetails, UserLevel) VALUES('" & cIdentity.Name.Trim & "', '', '" _
                & HashPasswordSha256(cIdentity.Password.Trim) & "', '','', 0);")

            Dim UID As Integer = ExecuteUsingConnection(Conn, myComm)

            myComm = New SQLCommand("RawSQL", "INSERT INTO roles(UserID, DatabaseName, " _
                & "RoleName, RoleLevel) VALUES(" & UID.ToString & ", '', '" _
                & Name_AdminRole & "', 3);")

            ExecuteUsingConnection(Conn, myComm)

        End Sub

        ''' <summary>
        ''' Tryes to open the connection. Returns open connection if succeded.
        ''' </summary>
        Private Function OpenConnection() As SQLiteConnection
            Return OpenConnection(CType(ApplicationContext.User.Identity, _
               Security.AccIdentity).ConnString)
        End Function

        Friend Shared Function OpenConnection(ByVal connectionString As String) As SQLiteConnection

            Dim Conn As New SQLiteConnection(connectionString)

            Try
                Conn.Open()
            Catch ex As Exception
                Conn.Dispose()
                Throw New Exception("Klaida. Nepavyko prisijungti prie duomenų bazės: " & ex.Message, ex)
            End Try

            Return Conn

        End Function

        ''' <summary>
        ''' If a transaction exists returns the SQLiteConnection object from Transaction object.
        ''' Else - returns a new SQLiteConnection object.
        ''' </summary>
        ''' <param name="Conn">SQLiteConnection object: if a transaction exists 
        ''' the existing object is used; else a new connection is opened.</param>
        ''' <param name="CurrentSqlCommand">SqlCommand object containing the SQL statement.</param>
        Private Function GetCommand(ByRef Conn As SQLiteConnection, _
            ByVal CurrentSqlCommand As SQLCommand) As SQLiteCommand

            Dim result As SQLiteCommand

            If TransactionExists() Then
                result = TransactionGetCommand()
                result.Parameters.Clear()
                CType(ApplicationContext.LocalContext.Item(LC_TransactionStatementListKey), List(Of String)). _
                    Add(CurrentSqlCommand.ToString)
                Conn = TransactionGetConnection()
            Else
                Conn = OpenConnection()
                result = New SQLiteCommand
                result.Connection = Conn
                result.CommandTimeout = GetSqlQueryTimeOut()
            End If

            AddWithSqlCommand(result, CurrentSqlCommand)

            Return result

        End Function

        ''' <summary>
        ''' Returns a new SQLiteCommand object using the connection provided 
        ''' and data from SqlCommand provided.
        ''' </summary>
        ''' <param name="Conn">Connection to use for the SQLiteCommand object.</param>
        ''' <param name="CurrentSqlCommand">SqlCommand object containing the SQL statement.</param>
        Private Function GetNewCommand(ByVal Conn As SQLiteConnection, _
            ByVal CurrentSqlCommand As SQLCommand) As SQLiteCommand

            Dim result As SQLiteCommand

            result = New SQLiteCommand
            result.Connection = Conn
            result.CommandTimeout = GetSqlQueryTimeOut()

            AddWithSqlCommand(result, CurrentSqlCommand)

            Return result

        End Function

        ''' <summary>
        ''' Adds the SQLiteCommand object with the statement and params from SqlCommand object provided.
        ''' </summary>
        ''' <param name="myComm">The SQLiteCommand object to which statement and params should be added.</param>
        ''' <param name="CurrentSqlCommand">SqlCommand object containing the statement and the params.</param>
        ''' <remarks></remarks>
        Private Sub AddWithSqlCommand(ByRef myComm As SQLiteCommand, _
            ByVal CurrentSqlCommand As SQLCommand)

            myComm.CommandText = CurrentSqlCommand.Sentence

            myComm.Parameters.Clear()
            For i As Integer = 1 To CurrentSqlCommand.ParamsCount

                Dim paramName As String = CurrentSqlCommand.Params(i - 1).Name
                If paramName.Contains("?") Then
                    myComm.CommandText = myComm.CommandText.Replace( _
                        paramName, paramName.Replace("?", "$"))
                    paramName = paramName.Replace("?", "$")
                End If

                If Not CurrentSqlCommand.Params(i - 1).ToBeReplaced Then

                    'If CurrentSqlCommand.Params(i - 1).ValueType Is GetType(DateTime) Then

                    '    If DirectCast(CurrentSqlCommand.Params(i - 1).Value, DateTime).TimeOfDay.Ticks = 0 Then

                    '        myComm.Parameters.AddWithValue(CurrentSqlCommand.Params(i - 1).Name, _
                    '            DateToDbString(DirectCast(CurrentSqlCommand.Params(i - 1).Value, DateTime)))

                    '    Else

                    '        myComm.Parameters.AddWithValue(CurrentSqlCommand.Params(i - 1).Name, _
                    '            DateTimeToDbString(DirectCast(CurrentSqlCommand.Params(i - 1).Value, DateTime)))

                    '    End If

                    'Else

                    '    myComm.Parameters.AddWithValue(CurrentSqlCommand.Params(i - 1).Name, _
                    '        CurrentSqlCommand.Params(i - 1).Value)

                    'End If

                    myComm.Parameters.AddWithValue(CurrentSqlCommand.Params(i - 1).Name, _
                        CurrentSqlCommand.Params(i - 1).Value)

                End If

            Next

            For i As Integer = 1 To CurrentSqlCommand.ParamsCount
                myComm.CommandText = myComm.CommandText.Replace(CurrentSqlCommand.Params(i - 1).Name.Replace("$", "?"), _
                    CurrentSqlCommand.Params(i - 1).Name)
            Next

        End Sub

        ''' <summary>
        ''' Ends distributed (interobject) SQLite transaction. I.e. disposes of connection and other objects.
        ''' </summary>
        Private Sub TransactionEnd()
            Try
                Dim myConn As SQLiteConnection = TransactionGetConnection()
                Dim myComm As SQLiteCommand = TransactionGetCommand()
                Dim myTrans As SQLiteTransaction = TransactionGetObject()
                If myConn.State = ConnectionState.Open Then myConn.Close() 'issivalom
                myConn.Dispose()
                myTrans.Dispose()
                myComm.Dispose()
            Catch ex As Exception
            End Try
            Try
                ApplicationContext.LocalContext.Remove(LC_TransactionObjectKey)
                ApplicationContext.LocalContext.Remove(LC_TransactionStatementListKey)
            Catch ex As Exception
            End Try
        End Sub

        ''' <summary>
        ''' Returns SQLite transaction object.
        ''' </summary>
        Private Function TransactionGetObject() As SQLiteTransaction
            Dim tmp As SQLiteCommand = CType(Csla.ApplicationContext.LocalContext.Item(LC_TransactionObjectKey), _
                SQLiteCommand)
            Return tmp.Transaction
        End Function

        ''' <summary>
        ''' Returns SQLite transaction command object.
        ''' </summary>
        Private Function TransactionGetCommand() As SQLiteCommand
            Return CType(Csla.ApplicationContext.LocalContext.Item(LC_TransactionObjectKey), SQLiteCommand)
        End Function

        ''' <summary>
        ''' Returns SQLite transaction connection object.
        ''' </summary>
        Private Function TransactionGetConnection() As SQLiteConnection
            Dim tmp As SQLiteCommand = CType(Csla.ApplicationContext.LocalContext.Item(LC_TransactionObjectKey), _
                SQLiteCommand)
            Return tmp.Connection
        End Function

        Private Function EncodeStringToSQLite(ByRef SourceData As String) As String
            ' Create two different encodings.
            Dim ascii As System.Text.Encoding = System.Text.Encoding.UTF8
            Dim [unicode] As System.Text.Encoding = System.Text.Encoding.Unicode

            ' Convert the string into a byte[].
            Dim unicodeBytes As Byte() = [unicode].GetBytes(SourceData)

            ' Perform the conversion from one encoding to the other.
            Dim asciiBytes As Byte() = System.Text.Encoding.Convert([unicode], ascii, unicodeBytes)

            ' Convert the new byte[] into a char[] and then into a string.
            ' This is a slightly different approach to converting to illustrate
            ' the use of GetCharCount/GetChars.
            Dim asciiChars(ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length) - 1) As Char
            ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0)
            Dim asciiString As New String(asciiChars)
            Return asciiString
        End Function

        Public Sub ChangePassword(ByVal NewPassword As String) _
            Implements ISqlCommandManager.ChangePassword

            If GetCurrentIdentity.IsLocalUser Then
                ChangeDbFilePassword(NewPassword)
            Else

                Dim myComm As New SQLCommand("RawSQL", "UPDATE users SET Password=$PS " _
                    & "WHERE TRIM(LOWER(Name))=TRIM(LOWER($NM));")
                myComm.AddParam("?PS", HashPasswordSha256(NewPassword.Trim))
                myComm.AddParam("?NM", GetCurrentIdentity.Name.Trim.ToLower)

                Using ChangedDb As New ChangedDatabase(GetCurrentIdentity.SecurityDatabase)
                    myComm.Execute()
                End Using

            End If

        End Sub

        Private Sub ChangeDbFilePassword(ByVal NewPassword As String)
            Using conn As New SQLiteConnection(GetCurrentIdentity.ConnString)
                conn.Open()
                conn.ChangePassword(NewPassword)
                conn.Close()
            End Using
        End Sub

        Public Sub DropDatabase(ByVal DatabaseName As String) _
            Implements ISqlCommandManager.DropDatabase

            IO.File.Delete(AppPath() & Path_FileServerDatabaseFiles & "\" & DatabaseName.Trim _
                    & Name_FileServerDatabaseFilesExtension)

        End Sub

        Public Sub FetchCompanyInfoData(ByVal DatabaseName As String, _
            ByRef CompanyName As String, ByRef CompanyID As String) _
            Implements ISqlCommandManager.FetchCompanyInfoData

            Try
                Using ChangedDb As New ChangedDatabase(DatabaseName.Trim)

                    Dim myComm As New SQLCommand("GetCompanyByDatabase")

                    Using myData As DataTable = myComm.Fetch
                        CompanyName = myData.Rows(0).Item(0).ToString.Trim
                        CompanyID = myData.Rows(0).Item(1).ToString.Trim
                    End Using

                End Using
            Catch ex As Exception
                If GetCurrentIdentity.IsLocalUser AndAlso ex.Message.Contains("encrypted") Then
                    CompanyName = GetCompanyNameByLocalDatabaseName(DatabaseName)
                    CompanyID = ""
                Else
                    Throw
                End If
            End Try

        End Sub

        Public Function GetAccesibleDatabaseList() As System.Collections.Generic.List(Of String) _
            Implements ISqlCommandManager.GetAccesibleDatabaseList

            Dim CurrentIdentity As Security.AccIdentity = GetCurrentIdentity()

            If CurrentIdentity.IsLocalUser Then Return FetchDatabasesLocal("")

            Dim CurrentUserIsAdmin As Boolean = CurrentIdentity.IsInRole(Name_AdminRole)

            Dim result As List(Of String) = FetchDatabasesLocal(CurrentIdentity.DatabaseNamingConvention)

            If Not CurrentUserIsAdmin Then

                Using ChangedDb As New ChangedDatabase(CurrentIdentity.SecurityDatabase)

                    Dim myComm As New SQLCommand("RawSQL", "SELECT DISTINCT R.DatabaseName " _
                        & "FROM roles R, users U WHERE R.UserID=U.ID AND LOWER(TRIM(U.Name))=$NM;")
                    myComm.AddParam("?NM", CurrentIdentity.Name.Trim.ToLower)

                    Using myData As DataTable = myComm.Fetch

                        If myData.Rows.Count < 1 Then
                            result.Clear()
                            Return result
                        End If

                        Dim IsAccessible As Boolean
                        For i As Integer = result.Count To 1 Step -1
                            IsAccessible = False
                            For Each dr As DataRow In myData.Rows
                                If Not dr.Item(0) Is Nothing AndAlso Not IsDBNull(dr.Item(0)) AndAlso _
                                    dr.Item(0).ToString.Trim.ToLower = result(i - 1).Trim.ToLower Then
                                    IsAccessible = True
                                    Exit For
                                End If
                            Next
                            If Not IsAccessible Then result.RemoveAt(i - 1)
                        Next

                    End Using

                End Using

            End If

            Return result

        End Function

        Private Function FetchDatabasesLocal(ByVal DatabaseNamingConvention As String) _
            As System.Collections.Generic.List(Of String)

            Dim di As IO.DirectoryInfo
            Dim aryFi As IO.FileInfo()
            Try
                di = New IO.DirectoryInfo(AppPath() & Path_FileServerDatabaseFiles)
                aryFi = di.GetFiles("*" & Name_FileServerDatabaseFilesExtension)
            Catch ex As Exception
                Throw New Exception("Klaida. Nėra prieigos prie duomenų bazių failų. " _
                    & "Kreipkitės į sistemos administratorių.", ex)
            End Try

            Dim result As New List(Of String)

            For Each fi As IO.FileInfo In aryFi

                If fi.Extension.Trim.ToLower = Name_FileServerDatabaseFilesExtension AndAlso _
                    (String.IsNullOrEmpty(DatabaseNamingConvention.Trim) OrElse _
                    IO.Path.GetFileNameWithoutExtension(fi.Name).Trim.ToLower.StartsWith( _
                    DatabaseNamingConvention.Trim.ToLower)) Then _
                    result.Add(IO.Path.GetFileNameWithoutExtension(fi.Name).Trim)

            Next

            Return result

        End Function

        Private Function GetCompanyNameByLocalDatabaseName(ByVal DbName As String) As String

            Dim result As String = ""
            For Each c As Char In DbName
                If Char.IsUpper(c) AndAlso String.IsNullOrEmpty(result) Then
                    result = result & c
                ElseIf Char.IsUpper(c) Then
                    result = result & " " & Char.ToLower(c)
                Else
                    result = result & c
                End If
            Next
            Return result.Trim

        End Function

        Public Function GetConnectionString(ByVal TargetIdentity As Security.AccIdentity) As String _
            Implements ISqlCommandManager.GetConnectionString

            If String.IsNullOrEmpty(TargetIdentity.Database.Trim) Then _
                Throw New Exception("Klaida. Nenurodyta SQLite duomenų bazė.")

            If TargetIdentity.IsLocalUser AndAlso Not Security.SecurityMethods.IsWebServer( _
                TargetIdentity.IsWebServerImpersonation) Then

                Dim filePath As String = IO.Path.Combine(IO.Path.Combine(AppPath(), "Data"),
                    TargetIdentity.Database & Name_FileServerDatabaseFilesExtension)

                IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(filePath))

                If String.IsNullOrEmpty(TargetIdentity.Password.Trim) Then
                    Return String.Format(ConnString_WithoutPassword, filePath)
                Else
                    Return String.Format(ConnString, filePath, TargetIdentity.Password)
                End If

            Else

                Return ConfigurationManager.ConnectionStrings( _
                    AppSettingsKey_SqlConnectionString).ConnectionString.Replace( _
                    Name_DatabaseTagInConnString, TargetIdentity.Database)

            End If

        End Function

        Public Function GetSqlDepositoryFileName() As String _
            Implements ISqlCommandManager.GetSqlDepositoryFileName
            Return AppPath() & Path_SQLiteDepositoryFile
        End Function

    End Class

End Namespace