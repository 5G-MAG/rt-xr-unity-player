//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (https://www.swig.org).
// Version 4.2.1
//
// Do not make changes to this file unless you know what you are doing - modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------

namespace maf {

public class Camera : global::System.IDisposable {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal Camera(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(Camera obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  internal static global::System.Runtime.InteropServices.HandleRef swigRelease(Camera obj) {
    if (obj != null) {
      if (!obj.swigCMemOwn)
        throw new global::System.ApplicationException("Cannot release ownership as memory is not owned");
      global::System.Runtime.InteropServices.HandleRef ptr = obj.swigCPtr;
      obj.swigCMemOwn = false;
      obj.Dispose();
      return ptr;
    } else {
      return new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
    }
  }

  ~Camera() {
    Dispose(false);
  }

  public void Dispose() {
    Dispose(true);
    global::System.GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing) {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          mafPINVOKE.delete_Camera(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
    }
  }

  public SWIGTYPE_p_MAF__CameraProjectionType type {
    set {
      mafPINVOKE.Camera_type_set(swigCPtr, SWIGTYPE_p_MAF__CameraProjectionType.getCPtr(value));
    } 
    get {
      global::System.IntPtr cPtr = mafPINVOKE.Camera_type_get(swigCPtr);
      SWIGTYPE_p_MAF__CameraProjectionType ret = (cPtr == global::System.IntPtr.Zero) ? null : new SWIGTYPE_p_MAF__CameraProjectionType(cPtr, false);
      return ret;
    } 
  }

  public PerspectiveCameraViewingVolume perspectiveCamViewVolume {
    set {
      mafPINVOKE.Camera_perspectiveCamViewVolume_set(swigCPtr, PerspectiveCameraViewingVolume.getCPtr(value));
    } 
    get {
      global::System.IntPtr cPtr = mafPINVOKE.Camera_perspectiveCamViewVolume_get(swigCPtr);
      PerspectiveCameraViewingVolume ret = (cPtr == global::System.IntPtr.Zero) ? null : new PerspectiveCameraViewingVolume(cPtr, false);
      return ret;
    } 
  }

  public OrthographicCameraViewingVolume orthographicCamViewVolume {
    set {
      mafPINVOKE.Camera_orthographicCamViewVolume_set(swigCPtr, OrthographicCameraViewingVolume.getCPtr(value));
    } 
    get {
      global::System.IntPtr cPtr = mafPINVOKE.Camera_orthographicCamViewVolume_get(swigCPtr);
      OrthographicCameraViewingVolume ret = (cPtr == global::System.IntPtr.Zero) ? null : new OrthographicCameraViewingVolume(cPtr, false);
      return ret;
    } 
  }

  public SWIGTYPE_p_double zNear {
    set {
      mafPINVOKE.Camera_zNear_set(swigCPtr, SWIGTYPE_p_double.getCPtr(value));
    } 
    get {
      global::System.IntPtr cPtr = mafPINVOKE.Camera_zNear_get(swigCPtr);
      SWIGTYPE_p_double ret = (cPtr == global::System.IntPtr.Zero) ? null : new SWIGTYPE_p_double(cPtr, false);
      return ret;
    } 
  }

  public SWIGTYPE_p_double zFar {
    set {
      mafPINVOKE.Camera_zFar_set(swigCPtr, SWIGTYPE_p_double.getCPtr(value));
    } 
    get {
      global::System.IntPtr cPtr = mafPINVOKE.Camera_zFar_get(swigCPtr);
      SWIGTYPE_p_double ret = (cPtr == global::System.IntPtr.Zero) ? null : new SWIGTYPE_p_double(cPtr, false);
      return ret;
    } 
  }

  public Camera() : this(mafPINVOKE.new_Camera(), true) {
  }

}

}
