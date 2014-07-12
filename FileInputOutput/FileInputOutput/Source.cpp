#include<tchar.h>
#include<stdio.h>
#include<fstream>
#include<iostream>
#include<WinSock2.h>
#include<Windows.h>

#define SERVERPORT  8172
#define SERVER_NAME "srvrdc01.geomartin.local"
#define RESULT_STRING "FAILURE"
#define FILE_PATH   "\\\\backups.geomartin.local\\computerimagingprimary\\resources\\ImageCreation.log"
#pragma comment(lib, "ws2_32.lib")

void messageCoordinator(char *, int);

/**
 * Write log messages to the specified log file and send result
 * via TCP sockets to the coordinating server using Windows API.
 */
int __cdecl _tmain()    
{
    DWORD       nameSize = _MAX_PATH;      
    TCHAR       netResult[_MAX_PATH], 
                name[_MAX_PATH],
                result[_MAX_PATH],
                data[_MAX_PATH];
    DWORD       bytesWritten;
    SYSTEMTIME  time;
    TCHAR       filename[]  =       _T(FILE_PATH); 

    GetSystemTime(&time); 
    GetComputerName(name, &nameSize);
    _stprintf(result, _T("Result: %s\n\r"), _T(RESULT_STRING));
    _stprintf(netResult, _T("%s:%s%s"), name, _T(RESULT_STRING), _T("<EOF>"));
    _stprintf(
        data, 
        _T("\n%2.2i-%2.2i-%4.4i, %2.2i-%2.2i: Hostname: %s, %s"), 
        time.wMonth, time.wDay, time.wYear, time.wHour, time.wMinute,
        name, result
    );

    HANDLE file = CreateFile(
        filename,
        FILE_APPEND_DATA,
        0,
        NULL,
        OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL
    );
    
    if (file == INVALID_HANDLE_VALUE) { 
        wprintf(L"File handle was invalid");    
        return EXIT_FAILURE; 
    }
    WriteFile(file, data, _tcslen(data)*sizeof(TCHAR), &bytesWritten, NULL);    
    CloseHandle(file);
    messageCoordinator((char *)netResult, _tcslen(netResult));
    return EXIT_SUCCESS;
}

/**
 * Send a TCP message to the coordinator server to report backup stats.
 *
 * @param   message     To send to the coordinator.
 * @param   len         Length (num chars) of the message.
 */
void messageCoordinator(char *message, int len)
{
    WSADATA     wsa;
    char*       server_ip;
    int         sock_desc; 
    struct  sockaddr_in server;
    struct  hostent*    host;
    
    if (WSAStartup(MAKEWORD(2,2),&wsa) != 0) {
        _tprintf(_T("ERROR: Couldn't init Winsock %i"), WSAGetLastError());
        return;
    }
    
    if ((sock_desc = socket(PF_INET, SOCK_STREAM, IPPROTO_TCP)) == INVALID_SOCKET) {
        _tprintf(_T("Error: Couldn't create socket: %d"), WSAGetLastError());
        return;
    }

    if((host = gethostbyname(SERVER_NAME)) == NULL) {
        _tprintf(_T("Couldn't resolve hostname, Err: %i\n"), WSAGetLastError());
        return;
    }

    server_ip = inet_ntoa(*(struct in_addr *) *host->h_addr_list);
    server.sin_family = AF_INET;
    server.sin_port = htons(SERVERPORT);
    server.sin_addr.s_addr = inet_addr(server_ip);
    
    if(connect(sock_desc, (struct sockaddr *)&server, sizeof(server)) < 0) {
        _tprintf(_T("Connection error %i\n"), WSAGetLastError());
        return;
    }

    if(send(sock_desc, message, len*sizeof(TCHAR), 0) < 0) {
        _tprintf(_T("Send failed with %i\n"), WSAGetLastError());
        return;
    }
   _tprintf(_T("Sent %d bytes %s"), len*sizeof(TCHAR), message);

   shutdown(sock_desc, SD_SEND);
}