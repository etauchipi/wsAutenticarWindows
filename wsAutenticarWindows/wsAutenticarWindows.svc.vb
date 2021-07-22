Imports System.Text
Imports System.Collections
Imports System.DirectoryServices


' NOTA: puede usar el comando "Cambiar nombre" del menú contextual para cambiar el nombre de clase "Service1" en el código, en svc y en el archivo de configuración.
' NOTA: para iniciar el Cliente de prueba WCF para probar este servicio, seleccione Service1.svc o Service1.svc.vb en el Explorador de soluciones e inicie la depuración.
<ServiceBehavior(InstanceContextMode:=InstanceContextMode.PerCall, IncludeExceptionDetailInFaults:=True)>
Public Class wsAutenticarWindows
    Implements IwsAutenticarWindows

    Dim s_path As String
    Dim s_filterAttribute As String
    Dim adAuth As AuthenticationTypes

    Public Function Autenticar(ByVal domain As String, ByVal username As String, ByVal pwd As String) As Boolean Implements IwsAutenticarWindows.Autenticar

        Dim retorno As Boolean

        retorno = False
        s_path = ConfigurationManager.AppSettings("LDAP_path")
        s_filterAttribute = String.Empty
        retorno = IsAuthenticated(domain, username, pwd)

        Return retorno

    End Function

    Private Function IsAuthenticated(ByVal domain As String, ByVal username As String, ByVal pwd As String) As Boolean

        Dim domainAndUsername As String
        Dim entry As DirectoryEntry
        Dim retorno As Boolean
        Dim search As DirectorySearcher
        Dim result As SearchResult

        domainAndUsername = domain & "/" & username
        entry = New DirectoryEntry(s_path, domainAndUsername, pwd)
        retorno = False

        Try
            'Bind to the native AdsObject to force authentication.
            'Dim obj As Object = entry.NativeObject
            search = New DirectorySearcher(entry)

            search.Filter = "(SAMAccountName=" & username & ")"
            search.PropertiesToLoad.Add("cn")
            result = search.FindOne()

            If (result Is Nothing) Then
                retorno = False
            Else
                retorno = True
            End If

            'Update the new path to the user in the directory.
            s_path = result.Path
            s_filterAttribute = CType(result.Properties("cn")(0), String)

        Catch ex As Exception
            Throw New Exception("Error autenticando usuario. " & ex.Message)
        Finally

        End Try

        Return retorno

    End Function

    Private Function GetGroups() As String

        Dim retorno As String
        Dim search As DirectorySearcher = New DirectorySearcher(s_path)
        Dim groupNames As StringBuilder = New StringBuilder()

        search.Filter = "(cn=" & s_filterAttribute & ")"
        search.PropertiesToLoad.Add("memberOf")
        retorno = String.Empty

        Try
            Dim result As SearchResult = search.FindOne()
            Dim propertyCount As Integer = result.Properties("memberOf").Count

            Dim dn As String
            Dim equalsIndex, commaIndex
            Dim propertyCounter As Integer

            For propertyCounter = 0 To propertyCount - 1
                dn = CType(result.Properties("memberOf")(propertyCounter), String)

                equalsIndex = dn.IndexOf("=", 1)
                commaIndex = dn.IndexOf(",", 1)

                If (equalsIndex = -1) Then
                    retorno = String.Empty
                    Exit For
                End If

                groupNames.Append(dn.Substring((equalsIndex + 1), (commaIndex - equalsIndex) - 1))
                groupNames.Append("|")

            Next

            If (retorno = String.Empty) Then
            Else
                retorno = groupNames.ToString()
            End If

        Catch ex As Exception
            Throw New Exception("Error obteniendo nombres de grupo. " & ex.Message)
            retorno = String.Empty
        Finally

        End Try

        Return retorno

    End Function

End Class
