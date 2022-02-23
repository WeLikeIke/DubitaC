/*** Autogenerated by WIDL 6.4 from include/windows.security.cryptography.idl - Do not edit ***/

#ifdef _WIN32
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif
#include <rpc.h>
#include <rpcndr.h>
#endif

#ifndef COM_NO_WINDOWS_H
#include <windows.h>
#include <ole2.h>
#endif

#ifndef __windows_security_cryptography_h__
#define __windows_security_cryptography_h__

/* Forward declarations */

#ifndef ____x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_FWD_DEFINED__
#define ____x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_FWD_DEFINED__
typedef interface __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics;
#ifdef __cplusplus
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics ABI::Windows::Security::Cryptography::ICryptographicBufferStatics
namespace ABI {
    namespace Windows {
        namespace Security {
            namespace Cryptography {
                interface ICryptographicBufferStatics;
            }
        }
    }
}
#endif /* __cplusplus */
#endif

/* Headers for imported files */

#include <inspectable.h>
#include <windows.storage.streams.h>

#ifdef __cplusplus
extern "C" {
#endif

#ifndef ____x_ABI_CWindows_CStorage_CStreams_CIBuffer_FWD_DEFINED__
#define ____x_ABI_CWindows_CStorage_CStreams_CIBuffer_FWD_DEFINED__
typedef interface __x_ABI_CWindows_CStorage_CStreams_CIBuffer __x_ABI_CWindows_CStorage_CStreams_CIBuffer;
#ifdef __cplusplus
#define __x_ABI_CWindows_CStorage_CStreams_CIBuffer ABI::Windows::Storage::Streams::IBuffer
namespace ABI {
    namespace Windows {
        namespace Storage {
            namespace Streams {
                interface IBuffer;
            }
        }
    }
}
#endif /* __cplusplus */
#endif

#ifndef ____x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_FWD_DEFINED__
#define ____x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_FWD_DEFINED__
typedef interface __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics;
#ifdef __cplusplus
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics ABI::Windows::Security::Cryptography::ICryptographicBufferStatics
namespace ABI {
    namespace Windows {
        namespace Security {
            namespace Cryptography {
                interface ICryptographicBufferStatics;
            }
        }
    }
}
#endif /* __cplusplus */
#endif

#ifdef __cplusplus
} /* extern "C" */
namespace ABI {
    namespace Windows {
        namespace Security {
            namespace Cryptography {
                enum BinaryStringEncoding {
                    BinaryStringEncoding_Utf8 = 0,
                    BinaryStringEncoding_Utf16LE = 1,
                    BinaryStringEncoding_Utf16BE = 2
                };
            }
        }
    }
}
extern "C" {
#else
enum __x_ABI_CWindows_CSecurity_CCryptography_CBinaryStringEncoding {
    BinaryStringEncoding_Utf8 = 0,
    BinaryStringEncoding_Utf16LE = 1,
    BinaryStringEncoding_Utf16BE = 2
};
#ifdef WIDL_using_Windows_Security_Cryptography
#define BinaryStringEncoding __x_ABI_CWindows_CSecurity_CCryptography_CBinaryStringEncoding
#endif /* WIDL_using_Windows_Security_Cryptography */
#endif

/*****************************************************************************
 * ICryptographicBufferStatics interface
 */
#ifndef ____x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_INTERFACE_DEFINED__
#define ____x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_INTERFACE_DEFINED__

DEFINE_GUID(IID___x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics, 0x320b7e22, 0x3cb0, 0x4cdf, 0x86,0x63, 0x1d,0x28,0x91,0x00,0x65,0xeb);
#if defined(__cplusplus) && !defined(CINTERFACE)
} /* extern "C" */
namespace ABI {
    namespace Windows {
        namespace Security {
            namespace Cryptography {
                MIDL_INTERFACE("320b7e22-3cb0-4cdf-8663-1d28910065eb")
                ICryptographicBufferStatics : public IInspectable
                {
                    virtual HRESULT STDMETHODCALLTYPE Compare(
                        ABI::Windows::Storage::Streams::IBuffer *object1,
                        ABI::Windows::Storage::Streams::IBuffer *object2,
                        boolean *isEqual) = 0;

                    virtual HRESULT STDMETHODCALLTYPE GenerateRandom(
                        UINT32 length,
                        ABI::Windows::Storage::Streams::IBuffer **buffer) = 0;

                    virtual HRESULT STDMETHODCALLTYPE GenerateRandomNumber(
                        UINT32 *value) = 0;

                    virtual HRESULT STDMETHODCALLTYPE CreateFromByteArray(
                        UINT32 __valueSize,
                        BYTE *value,
                        ABI::Windows::Storage::Streams::IBuffer **buffer) = 0;

                    virtual HRESULT STDMETHODCALLTYPE CopyToByteArray(
                        ABI::Windows::Storage::Streams::IBuffer *buffer,
                        UINT32 *__valueSize,
                        BYTE **value) = 0;

                    virtual HRESULT STDMETHODCALLTYPE DecodeFromHexString(
                        HSTRING value,
                        ABI::Windows::Storage::Streams::IBuffer **buffer) = 0;

                    virtual HRESULT STDMETHODCALLTYPE EncodeToHexString(
                        ABI::Windows::Storage::Streams::IBuffer *buffer,
                        HSTRING *value) = 0;

                    virtual HRESULT STDMETHODCALLTYPE DecodeFromBase64String(
                        HSTRING value,
                        ABI::Windows::Storage::Streams::IBuffer **buffer) = 0;

                    virtual HRESULT STDMETHODCALLTYPE EncodeToBase64String(
                        ABI::Windows::Storage::Streams::IBuffer *buffer,
                        HSTRING *value) = 0;

                    virtual HRESULT STDMETHODCALLTYPE ConvertStringToBinary(
                        HSTRING value,
                        enum BinaryStringEncoding encoding,
                        ABI::Windows::Storage::Streams::IBuffer **buffer) = 0;

                    virtual HRESULT STDMETHODCALLTYPE ConvertBinaryToString(
                        enum BinaryStringEncoding encoding,
                        ABI::Windows::Storage::Streams::IBuffer *buffer,
                        HSTRING *value) = 0;

                };
            }
        }
    }
}
extern "C" {
#ifdef __CRT_UUID_DECL
__CRT_UUID_DECL(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics, 0x320b7e22, 0x3cb0, 0x4cdf, 0x86,0x63, 0x1d,0x28,0x91,0x00,0x65,0xeb)
#endif
#else
typedef struct __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStaticsVtbl {
    BEGIN_INTERFACE

    /*** IUnknown methods ***/
    HRESULT (STDMETHODCALLTYPE *QueryInterface)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        REFIID riid,
        void **ppvObject);

    ULONG (STDMETHODCALLTYPE *AddRef)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This);

    ULONG (STDMETHODCALLTYPE *Release)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This);

    /*** IInspectable methods ***/
    HRESULT (STDMETHODCALLTYPE *GetIids)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        ULONG *iidCount,
        IID **iids);

    HRESULT (STDMETHODCALLTYPE *GetRuntimeClassName)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        HSTRING *className);

    HRESULT (STDMETHODCALLTYPE *GetTrustLevel)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        TrustLevel *trustLevel);

    /*** ICryptographicBufferStatics methods ***/
    HRESULT (STDMETHODCALLTYPE *Compare)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        __x_ABI_CWindows_CStorage_CStreams_CIBuffer *object1,
        __x_ABI_CWindows_CStorage_CStreams_CIBuffer *object2,
        boolean *isEqual);

    HRESULT (STDMETHODCALLTYPE *GenerateRandom)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        UINT32 length,
        __x_ABI_CWindows_CStorage_CStreams_CIBuffer **buffer);

    HRESULT (STDMETHODCALLTYPE *GenerateRandomNumber)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        UINT32 *value);

    HRESULT (STDMETHODCALLTYPE *CreateFromByteArray)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        UINT32 __valueSize,
        BYTE *value,
        __x_ABI_CWindows_CStorage_CStreams_CIBuffer **buffer);

    HRESULT (STDMETHODCALLTYPE *CopyToByteArray)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        __x_ABI_CWindows_CStorage_CStreams_CIBuffer *buffer,
        UINT32 *__valueSize,
        BYTE **value);

    HRESULT (STDMETHODCALLTYPE *DecodeFromHexString)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        HSTRING value,
        __x_ABI_CWindows_CStorage_CStreams_CIBuffer **buffer);

    HRESULT (STDMETHODCALLTYPE *EncodeToHexString)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        __x_ABI_CWindows_CStorage_CStreams_CIBuffer *buffer,
        HSTRING *value);

    HRESULT (STDMETHODCALLTYPE *DecodeFromBase64String)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        HSTRING value,
        __x_ABI_CWindows_CStorage_CStreams_CIBuffer **buffer);

    HRESULT (STDMETHODCALLTYPE *EncodeToBase64String)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        __x_ABI_CWindows_CStorage_CStreams_CIBuffer *buffer,
        HSTRING *value);

    HRESULT (STDMETHODCALLTYPE *ConvertStringToBinary)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        HSTRING value,
        enum __x_ABI_CWindows_CSecurity_CCryptography_CBinaryStringEncoding encoding,
        __x_ABI_CWindows_CStorage_CStreams_CIBuffer **buffer);

    HRESULT (STDMETHODCALLTYPE *ConvertBinaryToString)(
        __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics *This,
        enum __x_ABI_CWindows_CSecurity_CCryptography_CBinaryStringEncoding encoding,
        __x_ABI_CWindows_CStorage_CStreams_CIBuffer *buffer,
        HSTRING *value);

    END_INTERFACE
} __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStaticsVtbl;

interface __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics {
    CONST_VTBL __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStaticsVtbl* lpVtbl;
};

#ifdef COBJMACROS
#ifndef WIDL_C_INLINE_WRAPPERS
/*** IUnknown methods ***/
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_QueryInterface(This,riid,ppvObject) (This)->lpVtbl->QueryInterface(This,riid,ppvObject)
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_AddRef(This) (This)->lpVtbl->AddRef(This)
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_Release(This) (This)->lpVtbl->Release(This)
/*** IInspectable methods ***/
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GetIids(This,iidCount,iids) (This)->lpVtbl->GetIids(This,iidCount,iids)
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GetRuntimeClassName(This,className) (This)->lpVtbl->GetRuntimeClassName(This,className)
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GetTrustLevel(This,trustLevel) (This)->lpVtbl->GetTrustLevel(This,trustLevel)
/*** ICryptographicBufferStatics methods ***/
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_Compare(This,object1,object2,isEqual) (This)->lpVtbl->Compare(This,object1,object2,isEqual)
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GenerateRandom(This,length,buffer) (This)->lpVtbl->GenerateRandom(This,length,buffer)
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GenerateRandomNumber(This,value) (This)->lpVtbl->GenerateRandomNumber(This,value)
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_CreateFromByteArray(This,__valueSize,value,buffer) (This)->lpVtbl->CreateFromByteArray(This,__valueSize,value,buffer)
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_CopyToByteArray(This,buffer,__valueSize,value) (This)->lpVtbl->CopyToByteArray(This,buffer,__valueSize,value)
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_DecodeFromHexString(This,value,buffer) (This)->lpVtbl->DecodeFromHexString(This,value,buffer)
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_EncodeToHexString(This,buffer,value) (This)->lpVtbl->EncodeToHexString(This,buffer,value)
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_DecodeFromBase64String(This,value,buffer) (This)->lpVtbl->DecodeFromBase64String(This,value,buffer)
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_EncodeToBase64String(This,buffer,value) (This)->lpVtbl->EncodeToBase64String(This,buffer,value)
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_ConvertStringToBinary(This,value,encoding,buffer) (This)->lpVtbl->ConvertStringToBinary(This,value,encoding,buffer)
#define __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_ConvertBinaryToString(This,encoding,buffer,value) (This)->lpVtbl->ConvertBinaryToString(This,encoding,buffer,value)
#else
/*** IUnknown methods ***/
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_QueryInterface(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,REFIID riid,void **ppvObject) {
    return This->lpVtbl->QueryInterface(This,riid,ppvObject);
}
static FORCEINLINE ULONG __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_AddRef(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This) {
    return This->lpVtbl->AddRef(This);
}
static FORCEINLINE ULONG __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_Release(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This) {
    return This->lpVtbl->Release(This);
}
/*** IInspectable methods ***/
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GetIids(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,ULONG *iidCount,IID **iids) {
    return This->lpVtbl->GetIids(This,iidCount,iids);
}
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GetRuntimeClassName(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,HSTRING *className) {
    return This->lpVtbl->GetRuntimeClassName(This,className);
}
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GetTrustLevel(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,TrustLevel *trustLevel) {
    return This->lpVtbl->GetTrustLevel(This,trustLevel);
}
/*** ICryptographicBufferStatics methods ***/
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_Compare(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,__x_ABI_CWindows_CStorage_CStreams_CIBuffer *object1,__x_ABI_CWindows_CStorage_CStreams_CIBuffer *object2,boolean *isEqual) {
    return This->lpVtbl->Compare(This,object1,object2,isEqual);
}
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GenerateRandom(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,UINT32 length,__x_ABI_CWindows_CStorage_CStreams_CIBuffer **buffer) {
    return This->lpVtbl->GenerateRandom(This,length,buffer);
}
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GenerateRandomNumber(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,UINT32 *value) {
    return This->lpVtbl->GenerateRandomNumber(This,value);
}
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_CreateFromByteArray(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,UINT32 __valueSize,BYTE *value,__x_ABI_CWindows_CStorage_CStreams_CIBuffer **buffer) {
    return This->lpVtbl->CreateFromByteArray(This,__valueSize,value,buffer);
}
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_CopyToByteArray(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,__x_ABI_CWindows_CStorage_CStreams_CIBuffer *buffer,UINT32 *__valueSize,BYTE **value) {
    return This->lpVtbl->CopyToByteArray(This,buffer,__valueSize,value);
}
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_DecodeFromHexString(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,HSTRING value,__x_ABI_CWindows_CStorage_CStreams_CIBuffer **buffer) {
    return This->lpVtbl->DecodeFromHexString(This,value,buffer);
}
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_EncodeToHexString(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,__x_ABI_CWindows_CStorage_CStreams_CIBuffer *buffer,HSTRING *value) {
    return This->lpVtbl->EncodeToHexString(This,buffer,value);
}
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_DecodeFromBase64String(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,HSTRING value,__x_ABI_CWindows_CStorage_CStreams_CIBuffer **buffer) {
    return This->lpVtbl->DecodeFromBase64String(This,value,buffer);
}
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_EncodeToBase64String(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,__x_ABI_CWindows_CStorage_CStreams_CIBuffer *buffer,HSTRING *value) {
    return This->lpVtbl->EncodeToBase64String(This,buffer,value);
}
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_ConvertStringToBinary(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,HSTRING value,enum __x_ABI_CWindows_CSecurity_CCryptography_CBinaryStringEncoding encoding,__x_ABI_CWindows_CStorage_CStreams_CIBuffer **buffer) {
    return This->lpVtbl->ConvertStringToBinary(This,value,encoding,buffer);
}
static FORCEINLINE HRESULT __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_ConvertBinaryToString(__x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics* This,enum __x_ABI_CWindows_CSecurity_CCryptography_CBinaryStringEncoding encoding,__x_ABI_CWindows_CStorage_CStreams_CIBuffer *buffer,HSTRING *value) {
    return This->lpVtbl->ConvertBinaryToString(This,encoding,buffer,value);
}
#endif
#ifdef WIDL_using_Windows_Security_Cryptography
#define IID_ICryptographicBufferStatics IID___x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics
#define ICryptographicBufferStaticsVtbl __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStaticsVtbl
#define ICryptographicBufferStatics __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics
#define ICryptographicBufferStatics_QueryInterface __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_QueryInterface
#define ICryptographicBufferStatics_AddRef __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_AddRef
#define ICryptographicBufferStatics_Release __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_Release
#define ICryptographicBufferStatics_GetIids __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GetIids
#define ICryptographicBufferStatics_GetRuntimeClassName __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GetRuntimeClassName
#define ICryptographicBufferStatics_GetTrustLevel __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GetTrustLevel
#define ICryptographicBufferStatics_Compare __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_Compare
#define ICryptographicBufferStatics_GenerateRandom __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GenerateRandom
#define ICryptographicBufferStatics_GenerateRandomNumber __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_GenerateRandomNumber
#define ICryptographicBufferStatics_CreateFromByteArray __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_CreateFromByteArray
#define ICryptographicBufferStatics_CopyToByteArray __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_CopyToByteArray
#define ICryptographicBufferStatics_DecodeFromHexString __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_DecodeFromHexString
#define ICryptographicBufferStatics_EncodeToHexString __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_EncodeToHexString
#define ICryptographicBufferStatics_DecodeFromBase64String __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_DecodeFromBase64String
#define ICryptographicBufferStatics_EncodeToBase64String __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_EncodeToBase64String
#define ICryptographicBufferStatics_ConvertStringToBinary __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_ConvertStringToBinary
#define ICryptographicBufferStatics_ConvertBinaryToString __x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_ConvertBinaryToString
#endif /* WIDL_using_Windows_Security_Cryptography */
#endif

#endif

#endif  /* ____x_ABI_CWindows_CSecurity_CCryptography_CICryptographicBufferStatics_INTERFACE_DEFINED__ */

/* Begin additional prototypes for all interfaces */

ULONG           __RPC_USER HSTRING_UserSize     (ULONG *, ULONG, HSTRING *);
unsigned char * __RPC_USER HSTRING_UserMarshal  (ULONG *, unsigned char *, HSTRING *);
unsigned char * __RPC_USER HSTRING_UserUnmarshal(ULONG *, unsigned char *, HSTRING *);
void            __RPC_USER HSTRING_UserFree     (ULONG *, HSTRING *);

/* End additional prototypes */

#ifdef __cplusplus
}
#endif

#endif /* __windows_security_cryptography_h__ */