#include<Windows.h>
#include<tchar.h>
#include<stdio.h>
#include<WinSock.h>
#include<fstream>
#include<iostream>

#define SERVERPORT 8172

void messageCoordinator(TCHAR *message);

/**
 * Write log messages to the specified log file and send result
 * via TCP sockets to the coordinating server using Windows API.
 */
int __cdecl _tmain()
{
    TCHAR filename[] = _T("\\\\backups.geomartin.local\\computerimagingprimary\\resources\\ImageCreation.log");
    TCHAR name[32767];
    TCHAR netResult[32767];
    DWORD nameSize = 32767;      
    DWORD bytesWritten;
    SYSTEMTIME time;
    TCHAR data[sizeof(TCHAR) * 1024] = _T("");
    TCHAR result[] = _T("Result: Success\r\n");
    GetSystemTime(&time);                            
    GetComputerName(&(*name), &nameSize);
    _stprintf_s(
        data, 
        _T("%2.2i-%2.2i-%4.4i, %2.2i-%2.2i: "), 
        time.wMonth, 
        time.wDay, 
        time.wYear, 
        time.wHour, 
        time.wMinute
    );
    _tcscat_s(data, _T("Hostname: "));
    _tcscat_s(data, &(*name));
    _tcscat_s(data, _T(", "));
    _tcscat_s(data, result);
    _tcscat_s(netResult, name);
    _tcscat_s(netResult, _T(":"));
    _tcscat_s(netResult, _T("SUCCESS"));

    HANDLE file = CreateFile(
        filename,
        FILE_APPEND_DATA,
        0,
        NULL,
        OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL
    );
    wprintf(L"Writing %s\nTo %s\n", data, filename);
    
    if (file == INVALID_HANDLE_VALUE) { 
        wprintf(L"File handle was invalid");    
        return EXIT_FAILURE; 
    }
    WriteFile(file, data, _tcslen(data)*sizeof(TCHAR), &bytesWritten, NULL);    
    wprintf(L"Wrote %d bytes, closing file and exiting\n", bytesWritten);
    CloseHandle(file);

    messageCoordinator(netResult);
    return EXIT_SUCCESS;
}

void messageCoordinator(TCHAR *message)
{

    int sock_desc = socket(
        PF_INET, 
        SOCK_STREAM, 
        getprotobyname("tcp")->p_proto
    );

    // connect(sock_desc,)
}