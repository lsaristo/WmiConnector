// This file provides basic support to write to a log file located somwhere
// on disk. 
#include<Windows.h>
#include<tchar.h>
#include<stdio.h>
#include<fstream>
#include<iostream>
void writeFile(wchar_t*, wchar_t*);

int __cdecl _tmain()
{
    TCHAR filename[] = _T("\\\\backups.geomartin.local\\computerimagingprimary\\resources\\ImageCreation.log");
    TCHAR name[32767];
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
    return EXIT_SUCCESS;
}

void writeFile(wchar_t* filename, wchar_t* message)
{
    std::wofstream file;
    file.open(filename);
    file << message;
    file.close();
}