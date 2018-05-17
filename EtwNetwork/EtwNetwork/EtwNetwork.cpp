// ֳכאגםûי DLL-פאיכ.

//Microsoft-Windows-Kernel-Network provider
//https://msdn.microsoft.com/en-us/library/windows/desktop/aa364117(v=vs.85).aspx

#include <stdlib.h>
#include <locale.h>
#include <stdio.h>
#include <strsafe.h>
#include <Windows.h>

//Turns the DEFINE_GUID for EventTraceGuid into a const.
#define INITGUID

#include <guiddef.h>
#include <wbemidl.h>
#include <wmistr.h>
#include <evntrace.h>
#include <tdh.h>
#include <in6addr.h>

#pragma comment(lib, "Advapi32.lib")
#pragma comment(lib, "Ole32.lib")
#pragma comment(lib, "tdh.lib")

namespace EtwNetwork
{
	

DEFINE_GUID ( /* 3d6fa8d0-fe05-11d0-9dda-00c04fd7ba7c */
    ProcessProviderGuid,
    0x3d6fa8d0,
    0xfe05,
    0x11d0,
    0x9d, 0xda, 0x00, 0xc0, 0x4f, 0xd7, 0xba, 0x7c
  );

//TCP-IP Event
//https://msdn.microsoft.com/en-us/library/windows/desktop/aa364128%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396

DEFINE_GUID ( /* 9a280ac0-c8e0-11d1-84e2-00c04fb998a2 */
    TcpIpEventGuid,
    0x9a280ac0,
    0xc8e0,
    0x11d1,
    0x84, 0xe2, 0x00, 0xc0, 0x4f, 0xb9, 0x98, 0xa2
  );


DEFINE_GUID ( /*bf3a50c5-a9c9-4988-a005-2df0b7c80f80*/
    UdpIpEventGuid,
    0xbf3a50c5,
    0xa9c9,
    0x4988,
    0xa0, 0x05, 0x2d, 0xf0, 0xb7, 0xc8, 0x0f, 0x80
  );



/* Managed type declarations*/

public ref class EtwEventProperty //represents ETW event property
{
public: 
	System::String ^ name;
	System::String ^ value;
};

public ref class EtwEvent //represents ETW event
{
public: 
	System::Guid guid;
	System::Int32 version;
	System::Int32 type;
	System::DateTime timestamp;
	System::Collections::Generic::List<EtwEventProperty ^> ^ properties;

	EtwEvent()
	{
		properties = gcnew System::Collections::Generic::List<EtwEventProperty ^>(15);
	}

	virtual  System::String ^ ToString() override {
		System::Text::StringBuilder ^ sb=gcnew System::Text::StringBuilder(600);
		sb->AppendLine("*** Network Event ***");
		sb->AppendLine("GUID: "+guid.ToString());
		sb->AppendLine("Version: "+version.ToString());
		sb->AppendLine("Type: "+type.ToString());
		sb->AppendLine("Time: "+timestamp.ToString());

		for each (EtwEventProperty^ prop in properties)
		{
			sb->AppendLine(prop->name+": "+prop->value);
		}
		sb->AppendLine("********************");
		return sb->ToString();
	}
};

public delegate void EventDelegate( System::Object^ sender, EtwEvent^ e );


/* Function forward declarations */
DWORD GetEventInformation(PEVENT_RECORD pEvent, PTRACE_EVENT_INFO & pInfo);
PBYTE PrintProperties(PEVENT_RECORD pEvent, PTRACE_EVENT_INFO pInfo, DWORD PointerSize, USHORT i, PBYTE pUserData, PBYTE pEndOfUserData);
DWORD GetPropertyLength(PEVENT_RECORD pEvent, PTRACE_EVENT_INFO pInfo, USHORT i, PUSHORT PropertyLength);
DWORD GetArraySize(PEVENT_RECORD pEvent, PTRACE_EVENT_INFO pInfo, USHORT i, PUSHORT ArraySize);
DWORD GetMapInfo(PEVENT_RECORD pEvent, LPWSTR pMapName, DWORD DecodingSource, PEVENT_MAP_INFO & pMapInfo);
void RemoveTrailingSpace(PEVENT_MAP_INFO pMapInfo);
VOID WINAPI EventCallback(PEVENT_RECORD pEvent);
BOOL WINAPI BufferEventCallback(PEVENT_TRACE_LOGFILE buf);

/* ETW Functions */

// Prints the property.
PBYTE PrintProperties(PEVENT_RECORD pEvent, PTRACE_EVENT_INFO pInfo, DWORD PointerSize, USHORT i, PBYTE pUserData, 
					  PBYTE pEndOfUserData, EtwEvent ^ ev)
{
    TDHSTATUS status = ERROR_SUCCESS;
    USHORT PropertyLength = 0;
    DWORD FormattedDataSize = 0;
    USHORT UserDataConsumed = 0;
    USHORT UserDataLength = 0;
    LPWSTR pFormattedData = NULL;
    DWORD LastMember = 0;  // Last member of a structure
    USHORT ArraySize = 0;
    PEVENT_MAP_INFO pMapInfo = NULL;


    // Get the length of the property.

    status = GetPropertyLength(pEvent, pInfo, i, &PropertyLength);
    if (ERROR_SUCCESS != status)
    { 
        pUserData = NULL;
        goto cleanup;
    }

    // Get the size of the array if the property is an array.

    status = GetArraySize(pEvent, pInfo, i, &ArraySize);

    for (USHORT k = 0; k < ArraySize; k++)
    {
        // If the property is a structure, print the members of the structure.

        if ((pInfo->EventPropertyInfoArray[i].Flags & PropertyStruct) == PropertyStruct)
        {
            LastMember = pInfo->EventPropertyInfoArray[i].structType.StructStartIndex + 
                pInfo->EventPropertyInfoArray[i].structType.NumOfStructMembers;

            for (USHORT j = pInfo->EventPropertyInfoArray[i].structType.StructStartIndex; j < LastMember; j++)
            {
                pUserData = PrintProperties(pEvent, pInfo, PointerSize, j, pUserData, pEndOfUserData,ev);
                if (NULL == pUserData)
                {
                    //wprintf(L"Printing the members of the structure failed.\n");
                    pUserData = NULL;
                    goto cleanup;
                }
            }
        }
        else
        {
            // Get the name/value mapping if the property specifies a value map.

            status = GetMapInfo(pEvent, 
                (PWCHAR)((PBYTE)(pInfo) + pInfo->EventPropertyInfoArray[i].nonStructType.MapNameOffset),
                pInfo->DecodingSource,
                pMapInfo);

            if (ERROR_SUCCESS != status)
            {                
				//throw gcnew System::ComponentModel::Win32Exception(status,"GetMapInfo failed");
                pUserData = NULL;
                goto cleanup;
            }

            // Get the size of the buffer required for the formatted data.

            status = TdhFormatProperty(
                pInfo, 
                pMapInfo, 
                PointerSize, 
                pInfo->EventPropertyInfoArray[i].nonStructType.InType,
                pInfo->EventPropertyInfoArray[i].nonStructType.OutType,
                PropertyLength,
                (USHORT)(pEndOfUserData - pUserData),
                pUserData,
                &FormattedDataSize,
                pFormattedData,
                &UserDataConsumed);

            if (ERROR_INSUFFICIENT_BUFFER == status)
            {
                if (pFormattedData)
                {
                    free(pFormattedData);
                    pFormattedData = NULL;
                }

                pFormattedData = (LPWSTR) malloc(FormattedDataSize);
                if (pFormattedData == NULL)
                {                    
                    status = ERROR_OUTOFMEMORY;

					/*throw gcnew System::ComponentModel::Win32Exception(
						status,
						"Failed to allocate memory for formatted data"
						);*/

                    pUserData = NULL;
                    goto cleanup;
                }

                // Retrieve the formatted data.

                status = TdhFormatProperty(
                    pInfo, 
                    pMapInfo, 
                    PointerSize, 
                    pInfo->EventPropertyInfoArray[i].nonStructType.InType,
                    pInfo->EventPropertyInfoArray[i].nonStructType.OutType,
                    PropertyLength,
                    (USHORT)(pEndOfUserData - pUserData),
                    pUserData,
                    &FormattedDataSize,
                    pFormattedData,
                    &UserDataConsumed);
            }

            if (ERROR_SUCCESS == status)
            {                

				EtwEventProperty ^ prop = gcnew EtwEventProperty();
				prop->name = gcnew System::String(
					(PWCHAR)((PBYTE)(pInfo) + pInfo->EventPropertyInfoArray[i].NameOffset)
					);
				prop->value = gcnew System::String(pFormattedData);
				ev->properties->Add(prop);

                pUserData += UserDataConsumed;
            }
            else
            {                
				/*throw gcnew System::ComponentModel::Win32Exception(
						status,
						"TdhFormatProperty failed"
						);*/
                pUserData = NULL;
                goto cleanup;
            }
        }
    }

cleanup:

    if (pFormattedData)
    {
        free(pFormattedData);
        pFormattedData = NULL;
    }

    if (pMapInfo)
    {
        free(pMapInfo);
        pMapInfo = NULL;
    }

	if(status != ERROR_SUCCESS){
		throw gcnew System::ComponentModel::Win32Exception(status);
	}

    return pUserData;
}


// Get the length of the property data. 

DWORD GetPropertyLength(PEVENT_RECORD pEvent, PTRACE_EVENT_INFO pInfo, USHORT i, PUSHORT PropertyLength)
{
    DWORD status = ERROR_SUCCESS;
    PROPERTY_DATA_DESCRIPTOR DataDescriptor;
    DWORD PropertySize = 0;

    // If the property is a binary blob and is defined in a manifest, the property can 
    // specify the blob's size or it can point to another property that defines the 
    // blob's size. The PropertyParamLength flag tells you where the blob's size is defined.

    if ((pInfo->EventPropertyInfoArray[i].Flags & PropertyParamLength) == PropertyParamLength)
    {
        DWORD Length = 0;  // Expects the length to be defined by a UINT16 or UINT32
        DWORD j = pInfo->EventPropertyInfoArray[i].lengthPropertyIndex;
        ZeroMemory(&DataDescriptor, sizeof(PROPERTY_DATA_DESCRIPTOR));
        DataDescriptor.PropertyName = (ULONGLONG)((PBYTE)(pInfo) + pInfo->EventPropertyInfoArray[j].NameOffset);
        DataDescriptor.ArrayIndex = ULONG_MAX;
        status = TdhGetPropertySize(pEvent, 0, NULL, 1, &DataDescriptor, &PropertySize);
        status = TdhGetProperty(pEvent, 0, NULL, 1, &DataDescriptor, PropertySize, (PBYTE)&Length);
        *PropertyLength = (USHORT)Length;
    }
    else
    {
        if (pInfo->EventPropertyInfoArray[i].length > 0)
        {
            *PropertyLength = pInfo->EventPropertyInfoArray[i].length;
        }
        else
        {
            // If the property is a binary blob and is defined in a MOF class, the extension
            // qualifier is used to determine the size of the blob. However, if the extension 
            // is IPAddrV6, you must set the PropertyLength variable yourself because the 
            // EVENT_PROPERTY_INFO.length field will be zero.

            if (TDH_INTYPE_BINARY == pInfo->EventPropertyInfoArray[i].nonStructType.InType &&
                TDH_OUTTYPE_IPV6 == pInfo->EventPropertyInfoArray[i].nonStructType.OutType)
            {
                *PropertyLength = (USHORT)sizeof(IN6_ADDR);
            }
            else if (TDH_INTYPE_UNICODESTRING == pInfo->EventPropertyInfoArray[i].nonStructType.InType ||
                     TDH_INTYPE_ANSISTRING == pInfo->EventPropertyInfoArray[i].nonStructType.InType ||
                     (pInfo->EventPropertyInfoArray[i].Flags & PropertyStruct) == PropertyStruct)
            {
                *PropertyLength = pInfo->EventPropertyInfoArray[i].length;
            }
            else
            { 
                status = ERROR_EVT_INVALID_EVENT_DATA;
				/*throw gcnew System::ComponentModel::Win32Exception(
						status,
						"Unexpected length of structure"
						);*/
                goto cleanup;
            }
        }
    }

cleanup:

	if(status != ERROR_SUCCESS){
		throw gcnew System::ComponentModel::Win32Exception(status);
	}

    return status;
}


// Get the size of the array. 

DWORD GetArraySize(PEVENT_RECORD pEvent, PTRACE_EVENT_INFO pInfo, USHORT i, PUSHORT ArraySize)
{
    DWORD status = ERROR_SUCCESS;
    PROPERTY_DATA_DESCRIPTOR DataDescriptor;
    DWORD PropertySize = 0;

    if ((pInfo->EventPropertyInfoArray[i].Flags & PropertyParamCount) == PropertyParamCount)
    {
        DWORD Count = 0;  // Expects the count to be defined by a UINT16 or UINT32
        DWORD j = pInfo->EventPropertyInfoArray[i].countPropertyIndex;
        ZeroMemory(&DataDescriptor, sizeof(PROPERTY_DATA_DESCRIPTOR));
        DataDescriptor.PropertyName = (ULONGLONG)((PBYTE)(pInfo) + pInfo->EventPropertyInfoArray[j].NameOffset);
        DataDescriptor.ArrayIndex = ULONG_MAX;
        status = TdhGetPropertySize(pEvent, 0, NULL, 1, &DataDescriptor, &PropertySize);
        status = TdhGetProperty(pEvent, 0, NULL, 1, &DataDescriptor, PropertySize, (PBYTE)&Count);
        *ArraySize = (USHORT)Count;
    }
    else
    {
        *ArraySize = pInfo->EventPropertyInfoArray[i].count;
    }

    return status;
}


// Both MOF-based events and manifest-based events can specify name/value maps. The
// map values can be integer values or bit values. If the property specifies a value
// map, get the map.

DWORD GetMapInfo(PEVENT_RECORD pEvent, LPWSTR pMapName, DWORD DecodingSource, PEVENT_MAP_INFO & pMapInfo)
{
    DWORD status = ERROR_SUCCESS;
    DWORD MapSize = 0;

    // Retrieve the required buffer size for the map info.

    status = TdhGetEventMapInformation(pEvent, pMapName, pMapInfo, &MapSize);

    if (ERROR_INSUFFICIENT_BUFFER == status)
    {
        pMapInfo = (PEVENT_MAP_INFO) malloc(MapSize);
        if (pMapInfo == NULL)
        {
            //wprintf(L"Failed to allocate memory for map info (size=%lu).\n", MapSize);
            status = ERROR_OUTOFMEMORY;
            goto cleanup;
        }

        // Retrieve the map info.

        status = TdhGetEventMapInformation(pEvent, pMapName, pMapInfo, &MapSize);
    }

    if (ERROR_SUCCESS == status)
    {
        if (DecodingSourceXMLFile == DecodingSource)
        {
            RemoveTrailingSpace(pMapInfo);
        }
    }
    else
    {
        if  (ERROR_NOT_FOUND == status)
        {
            status = ERROR_SUCCESS; // This case is okay.
        }
        /*else
        {            
			throw gcnew System::ComponentModel::Win32Exception(
						status,
						"TdhGetEventMapInformation failed"
						);
        }*/
    }

cleanup:

	if(status != ERROR_SUCCESS){
		throw gcnew System::ComponentModel::Win32Exception(status);
	}
    return status;
}


// Replace the trailing space with a null-terminating character, so that the bit mapped strings are correctly formatted.

void RemoveTrailingSpace(PEVENT_MAP_INFO pMapInfo)
{
    DWORD ByteLength = 0;

    for (DWORD i = 0; i < pMapInfo->EntryCount; i++)
    {
        ByteLength = (wcslen((LPWSTR)((PBYTE)pMapInfo + pMapInfo->MapEntryArray[i].OutputOffset)) - 1) * 2;
        *((LPWSTR)((PBYTE)pMapInfo + (pMapInfo->MapEntryArray[i].OutputOffset + ByteLength))) = L'\0';
    }
}


// Get the metadata for the event.

DWORD GetEventInformation(PEVENT_RECORD pEvent, PTRACE_EVENT_INFO & pInfo)
{
    DWORD status = ERROR_SUCCESS;
    DWORD BufferSize = 0;

    // Retrieve the required buffer size for the event metadata.

    status = TdhGetEventInformation(pEvent, 0, NULL, pInfo, &BufferSize);

    if (ERROR_INSUFFICIENT_BUFFER == status)
    {
        pInfo = (TRACE_EVENT_INFO*) malloc(BufferSize);
        if (pInfo == NULL)
        {
            //wprintf(L"Failed to allocate memory for event info (size=%lu).\n", BufferSize);
            status = ERROR_OUTOFMEMORY;
			
            goto cleanup;
        }

        // Retrieve the event metadata.

        status = TdhGetEventInformation(pEvent, 0, NULL, pInfo, &BufferSize);
    }

    if (ERROR_SUCCESS != status)
    {
        //wprintf(L"TdhGetEventInformation failed with 0x%x.\n", status);
    }

cleanup:

	if(status != ERROR_SUCCESS){
		throw gcnew System::ComponentModel::Win32Exception(status);
	}

    return status;
}


/* ************ ETW Session ************ */

//global varaibles
TRACEHANDLE SessionHandle = 0;
EVENT_TRACE_PROPERTIES* pSessionProperties = NULL;
volatile BOOL fStop;

public ref class EtwSession 
{
public: 
	static event EventDelegate^ NewEvent;	
	static System::Boolean started = false;

	static void OnNewEvent(EtwEvent^ e)
	{
		NewEvent(gcnew System::Object(),e);
	}

static void Start(){

	if(started == true)return;
    ULONG status = ERROR_SUCCESS;  
    ULONG BufferSize = 0;
	EVENT_TRACE_LOGFILE trace={0};	
	fStop = FALSE;

    // Allocate memory for the session properties.

    BufferSize = sizeof(EVENT_TRACE_PROPERTIES) + sizeof(KERNEL_LOGGER_NAME);
    pSessionProperties = (EVENT_TRACE_PROPERTIES*) malloc(BufferSize);    
    if (NULL == pSessionProperties)
    {
        //wprintf(L"Unable to allocate %d bytes for properties structure.\n", BufferSize);
		status = E_OUTOFMEMORY;
        goto cleanup;
    }

    // Set the session properties. 

    ZeroMemory(pSessionProperties, BufferSize);
    pSessionProperties->Wnode.BufferSize = BufferSize;
    pSessionProperties->Wnode.Flags = WNODE_FLAG_TRACED_GUID;
    pSessionProperties->Wnode.ClientContext = 1; //QPC clock resolution
    pSessionProperties->Wnode.Guid = SystemTraceControlGuid; 
    pSessionProperties->EnableFlags = EVENT_TRACE_FLAG_NETWORK_TCPIP;
    pSessionProperties->LogFileMode = EVENT_TRACE_REAL_TIME_MODE;    
    pSessionProperties->LoggerNameOffset = sizeof(EVENT_TRACE_PROPERTIES);
    pSessionProperties->LogFileNameOffset = 0; 


    // Create the trace session.

    status = StartTrace((PTRACEHANDLE)&SessionHandle, KERNEL_LOGGER_NAME, pSessionProperties);

    if (ERROR_SUCCESS != status)
    {
        /*if (ERROR_ALREADY_EXISTS == status)
        {
            wprintf(L"The NT Kernel Logger session is already in use.\n");			
        }
        else
        {
            wprintf(L"StartTrace() failed with %lu\n", status);
        }*/

        goto cleanup;
    }

        
    TRACE_LOGFILE_HEADER* pHeader = &trace.LogfileHeader;
    ZeroMemory(&trace, sizeof(EVENT_TRACE_LOGFILE));
        trace.LoggerName = KERNEL_LOGGER_NAME;
    trace.LogFileName = (LPWSTR) NULL;
        trace.EventRecordCallback = (PEVENT_RECORD_CALLBACK) (EventCallback);
        trace.BufferCallback = (PEVENT_TRACE_BUFFER_CALLBACK)(BufferEventCallback);
        trace.ProcessTraceMode = PROCESS_TRACE_MODE_EVENT_RECORD | PROCESS_TRACE_MODE_REAL_TIME;

        // Open Trace
    TRACEHANDLE startTraceHandle = OpenTrace(&trace);
    if (INVALID_PROCESSTRACE_HANDLE == startTraceHandle)
    {
        status = GetLastError();
        //wprintf(L"KernelTraceSession: OpenTrace() failed with %lu\n", err);	
		goto cleanup;
    }

	started = true;
    status = ProcessTrace(&startTraceHandle, 1, 0, 0);    

cleanup:	

    Destroy();

	if(status != ERROR_SUCCESS && status != ERROR_CANCELLED){
		throw gcnew System::ComponentModel::Win32Exception(status);
	}
}

static void Stop(){
	fStop = TRUE;
}

static void Destroy(){
	ULONG status = ERROR_SUCCESS; 	

    if (SessionHandle)
    {
        status = ControlTrace(SessionHandle, KERNEL_LOGGER_NAME, pSessionProperties, EVENT_TRACE_CONTROL_STOP);
		SessionHandle = 0;        
    }

    if (pSessionProperties){
        free(pSessionProperties);
		pSessionProperties = NULL;
	}
	started = false;

	if(status != ERROR_SUCCESS){
		throw gcnew System::ComponentModel::Win32Exception(status);
	}
}

};
/* ************ end EtwSession ************ */


//Called on new ETW Event
VOID WINAPI EventCallback(PEVENT_RECORD pEvent)
{    

    DWORD status = ERROR_SUCCESS;
    PTRACE_EVENT_INFO pInfo = NULL;
    //LPWSTR pwsEventGuid = NULL;
    PBYTE pUserData = NULL;
    PBYTE pEndOfUserData = NULL;
    DWORD PointerSize = 0;
    ULONGLONG TimeStamp = 0;
    ULONGLONG Nanoseconds = 0;
    SYSTEMTIME st;
    SYSTEMTIME stLocal;
    FILETIME ft;
	EtwEvent ^ ev = gcnew EtwEvent();

    // Skips the event if it is the event trace header.

    if (IsEqualGUID(pEvent->EventHeader.ProviderId, EventTraceGuid) &&
        pEvent->EventHeader.EventDescriptor.Opcode == EVENT_TRACE_TYPE_INFO)
    {
        ; // Skip this event.
    }
    else
    {
        // Process the event. The pEvent->UserData member is a pointer to 
        // the event specific data, if it exists.

        status = GetEventInformation(pEvent, pInfo);

        if (ERROR_SUCCESS != status)
        {
            //wprintf(L"GetEventInformation failed with %lu\n", status);
            goto cleanup;
        }

        // Determine whether the event is defined by a MOF class, in an
        // instrumentation manifest, or a WPP template.

        if (DecodingSourceWbem == pInfo->DecodingSource)  // MOF class
        {
            //HRESULT hr = StringFromCLSID(pInfo->EventGuid, &pwsEventGuid);
			ev->guid = System::Guid (pInfo->EventGuid.Data1,pInfo->EventGuid.Data2,pInfo->EventGuid.Data3,
				pInfo->EventGuid.Data4[0],pInfo->EventGuid.Data4[1],pInfo->EventGuid.Data4[2],
				pInfo->EventGuid.Data4[3],pInfo->EventGuid.Data4[4],pInfo->EventGuid.Data4[5],
				pInfo->EventGuid.Data4[6],pInfo->EventGuid.Data4[7]);			        

           /*if(IsEqualGUID(pInfo->EventGuid,TcpIpEventGuid)!=FALSE){
                 wprintf(L"(TCP-IP Event)\n");
           }

		   if(IsEqualGUID(pInfo->EventGuid,UdpIpEventGuid)!=FALSE){
                 wprintf(L"(UDP-IP Event)\n");
           }	  */              

			ev->version = (int)(pEvent->EventHeader.EventDescriptor.Version);
			ev->type =  (int)(pEvent->EventHeader.EventDescriptor.Opcode);

        }
        else if (DecodingSourceXMLFile == pInfo->DecodingSource) // Instrumentation manifest
        {
            //wprintf(L"Event ID: %d\n", pInfo->EventDescriptor.Id);
			ev->type =  (int)(pInfo->EventDescriptor.Id);
        }
        else // Not handling the WPP case
        {
            goto cleanup;
        }

        // Print the time stamp for when the event occurred.

        ft.dwHighDateTime = pEvent->EventHeader.TimeStamp.HighPart;
        ft.dwLowDateTime = pEvent->EventHeader.TimeStamp.LowPart;

        FileTimeToSystemTime(&ft, &st);
        SystemTimeToTzSpecificLocalTime(NULL, &st, &stLocal);

        TimeStamp = pEvent->EventHeader.TimeStamp.QuadPart;
        Nanoseconds = (TimeStamp % 10000000) * 100;

        /*wprintf(L"%02d/%02d/%02d %02d:%02d:%02d.%I64u\n", 
            stLocal.wMonth, stLocal.wDay, stLocal.wYear, stLocal.wHour, stLocal.wMinute, stLocal.wSecond, Nanoseconds);*/
		ev->timestamp = System::DateTime(stLocal.wYear, stLocal.wMonth, stLocal.wDay,
			stLocal.wHour, stLocal.wMinute, stLocal.wSecond, Nanoseconds / 1000000);

        // If the event contains event-specific data use TDH to extract
        // the event data. 		       

        if (EVENT_HEADER_FLAG_32_BIT_HEADER == (pEvent->EventHeader.Flags & EVENT_HEADER_FLAG_32_BIT_HEADER))
        {
            PointerSize = 4;
        }
        else
        {
            PointerSize = 8;
        }


        pUserData = (PBYTE)pEvent->UserData;
        pEndOfUserData = (PBYTE)pEvent->UserData + pEvent->UserDataLength;

        // Print the event data for all the top-level properties. 				

        for (USHORT i = 0; i < pInfo->TopLevelPropertyCount; i++)
        {
            pUserData = PrintProperties(pEvent, pInfo, PointerSize, i, pUserData, pEndOfUserData,ev);
            if (NULL == pUserData)
            {
                //wprintf(L"Printing top level properties failed.\n");
                goto cleanup;
            }
        }

		System::String ^ str = ev->ToString();
		
		EtwSession::OnNewEvent(ev);
    }

cleanup:

    if (pInfo)
    {
        free(pInfo);
    }    

	if(status != ERROR_SUCCESS ){
		throw gcnew System::ComponentModel::Win32Exception(status);
	}
}

//return FALSE to end ETW Session
BOOL WINAPI BufferEventCallback(PEVENT_TRACE_LOGFILE buf)
{
        
		if(fStop != FALSE){
			fStop = FALSE;
			return FALSE;
		}
		else return TRUE;
}



} // END NAMESPACE

