using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace PSMoveSharp
{
    public struct PSMoveSharpConstants
    {
        public const int numButtons = 64;

        // CellPadData
        public const int offsetDigital1 = 2;
        public const int offsetDigital2 = 3;
        public const int offsetAnalogRightX = 4;
        public const int offsetAnalogRightY = 5;
        public const int offsetAnalogLeftX = 6;
        public const int offsetAnalogLeftY = 7;
        public const int offsetPressRight = 8;
        public const int offsetPressLeft = 9;
        public const int offsetPressUp = 10;
        public const int offsetPressDown = 11;
        public const int offsetPressTriangle = 12;
        public const int offsetPressCircle = 13;
        public const int offsetPressCross = 14;
        public const int offsetPressSquare = 15;
        public const int offsetPressL1 = 16;
        public const int offsetPressR1 = 17;
        public const int offsetPressL2 = 18;
        public const int offsetPressR2 = 19;
        public const int offsetSensorX = 20;
        public const int offsetSensorY = 21;
        public const int offsetSensorZ = 22;
        public const int offsetSensorG = 23;

        // Digital1
        public const int ctrlLeft = (1 << 7);
        public const int ctrlDown = (1 << 6);
        public const int ctrlRight = (1 << 5);
        public const int ctrlUp = (1 << 4);
        public const int ctrlStart = (1 << 3);
        public const int ctrlR3 = (1 << 2);
        public const int ctrlL3 = (1 << 1);
        public const int ctrlSelect = (1 << 0);

        // Digital2
        public const int ctrlSquare = (1 << 7);
        public const int ctrlCross = (1 << 6);
        public const int ctrlCircle = (1 << 5);
        public const int ctrlTriangle = (1 << 4);
        public const int ctrlR1 = (1 << 3);
        public const int ctrlL1 = (1 << 2);
        public const int ctrlR2 = (1 << 1);
        public const int ctrlL2 = (1 << 0);

        // Gem-only
        public const int ctrlTick = (1 << 2);
        public const int ctrlTrigger = (1 << 1);
    }

    public struct Float4
    {
        Float4(float _x, float _y, float _z, float _w)
        {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }
        public float x;
        public float y;
        public float z;
        public float w;
    }

    public struct NetworkWriterHelper {
        static public void WriteFloat(float f, ref byte[] buffer, int index) {
            byte[] f_bytes = System.BitConverter.GetBytes(f);
            Array.Reverse(f_bytes);
            buffer[index + 0] = f_bytes[0];
            buffer[index + 1] = f_bytes[1];
            buffer[index + 2] = f_bytes[2];
            buffer[index + 3] = f_bytes[3];
        }
    }

    public struct NetworkReaderHelper
    {
        static public byte ReadByte (ref byte[] buffer, int index)
        {
            return buffer[index];
        }

        static public UInt32 ReadUint32(ref byte[] buffer, int index)
        {
            UInt32 r;
            r = (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(buffer, index));
            return r;
        }

        static public UInt16 ReadUint16(ref byte[] buffer, int index)
        {
            UInt16 r;
            r = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(buffer, index));
            return r;
        }

        static public UInt64 ReadUint64(ref byte[] buffer, int index)
        {
            UInt64 r;
            r = (UInt64)IPAddress.NetworkToHostOrder((Int64)BitConverter.ToUInt64(buffer, index));
            return r;
        }

        static public Int32 ReadInt32(ref byte[] buffer, int index)
        {
            Int32 r;
            r = (Int32)IPAddress.NetworkToHostOrder((Int32)BitConverter.ToInt32(buffer, index));
            return r;
        }

        static public Int64 ReadInt64(ref byte[] buffer, int index)
        {
            Int64 r;
            r = (Int64)IPAddress.NetworkToHostOrder((Int64)BitConverter.ToInt64(buffer, index));
            return r;
        }

        static public float ReadFloat(ref byte[] buffer, int index)
        {
            float r;
            // copy bytes
            byte[] r_bytes = new byte[4];
            r_bytes[0] = buffer[index];
            r_bytes[1] = buffer[index + 1];
            r_bytes[2] = buffer[index + 2];
            r_bytes[3] = buffer[index + 3];
            // byte order conversion
            Array.Reverse(r_bytes);
            // convert to float
            r = BitConverter.ToSingle(r_bytes, 0);
            return r;
        }

        static public Float4 ReadFloat4(ref byte[] buffer, int index)
        {
            Float4 r;
            r.x = ReadFloat(ref buffer, index);
            r.y = ReadFloat(ref buffer, index + 4);
            r.z = ReadFloat(ref buffer, index + 8);
            r.w = ReadFloat(ref buffer, index + 12);
            return r;
        }
    }

    public struct PSMoveSharpServerConfig
    {
        public Int32 num_image_slices;
        public Int32 image_slice_format;
        public void FillFromNetworkBuffer(ref Byte[] buffer)
        {
            int offset = 24;
            num_image_slices = NetworkReaderHelper.ReadInt32(ref buffer, offset);
            image_slice_format = NetworkReaderHelper.ReadInt32(ref buffer, offset + 4);
        }
    }

    public struct PSMoveSharpStatus
    {

        public UInt32 connected;
        public UInt32 status_code;
        public UInt64 status_flags;
        public void FillFromNetworkBuffer(ref Byte[] buffer, int i)
        {
            int offset = 40 + i * 16;
            connected = NetworkReaderHelper.ReadUint32(ref buffer, offset);
            status_code = NetworkReaderHelper.ReadUint32(ref buffer, offset + 4);
            status_flags = NetworkReaderHelper.ReadUint64(ref buffer, offset + 8);
        }
    }

    public struct PSMoveSharpPadData
    {
        public UInt16 digitalbuttons;
        public UInt16 analog_trigger;
        public void FillFromNetworkBuffer(ref Byte[] buffer, int i)
        {
            int offset = 248 + i * 176;
            digitalbuttons = NetworkReaderHelper.ReadUint16(ref buffer, offset);
            analog_trigger = NetworkReaderHelper.ReadUint16(ref buffer, offset + 2);
        }
    }

    public struct PSMoveSharpGemState
    {
        public Float4 pos;
        public Float4 vel;
        public Float4 accel;
        public Float4 quat;
        public Float4 angvel;
        public Float4 angaccel;
        public Float4 handle_pos;
        public Float4 handle_vel;
        public Float4 handle_accel;
        public PSMoveSharpPadData pad; // 4 bytes
        public Int64 timestamp;
        public float temperature;
        public float camera_pitch_angle;
        public UInt32 tracking_flags;
        public void FillFromNetworkBuffer(ref Byte[] buffer, int i)
        {
            int offset = 104 + i * 176;
            pos = NetworkReaderHelper.ReadFloat4(ref buffer, offset);
            vel = NetworkReaderHelper.ReadFloat4(ref buffer, offset + 16);
            accel = NetworkReaderHelper.ReadFloat4(ref buffer, offset + 32);
            quat = NetworkReaderHelper.ReadFloat4(ref buffer, offset + 48);
            angvel = NetworkReaderHelper.ReadFloat4(ref buffer, offset + 64);
            angaccel = NetworkReaderHelper.ReadFloat4(ref buffer, offset + 80);
            handle_pos = NetworkReaderHelper.ReadFloat4(ref buffer, offset + 96);
            handle_vel = NetworkReaderHelper.ReadFloat4(ref buffer, offset + 112);
            handle_accel = NetworkReaderHelper.ReadFloat4(ref buffer, offset + 128);
            pad.FillFromNetworkBuffer(ref buffer, i);
            timestamp = NetworkReaderHelper.ReadInt64(ref buffer, offset + 152);
            temperature = NetworkReaderHelper.ReadFloat(ref buffer, offset + 160);
            camera_pitch_angle = NetworkReaderHelper.ReadFloat(ref buffer, offset + 164);
            tracking_flags = NetworkReaderHelper.ReadUint32(ref buffer, offset + 168);
        }
    }

    public struct PSMoveSharpImageState
    {
        public Int64 frame_timestamp;
        public Int64 timestamp;
        public float u;
        public float v;
        public float r;
        public float projectionx;
        public float projectiony;
        public float distance;
        public byte visible;
        public byte r_valid;
        public void FillFromNetworkBuffer(ref Byte[] buffer, int i)
        {
            int offset = 808 + i * 48;
            frame_timestamp = NetworkReaderHelper.ReadInt64(ref buffer, offset);
            timestamp = NetworkReaderHelper.ReadInt64(ref buffer, offset + 8);
            u = NetworkReaderHelper.ReadFloat(ref buffer, offset + 16);
            v = NetworkReaderHelper.ReadFloat(ref buffer, offset + 20);
            r = NetworkReaderHelper.ReadFloat(ref buffer, offset + 24);
            projectionx = NetworkReaderHelper.ReadFloat(ref buffer, offset + 28);
            projectiony = NetworkReaderHelper.ReadFloat(ref buffer, offset + 32);
            distance = NetworkReaderHelper.ReadFloat(ref buffer, offset + 36);
            visible = NetworkReaderHelper.ReadByte(ref buffer, offset + 40);
            r_valid = NetworkReaderHelper.ReadByte(ref buffer, offset + 31);
        }
    }

    public struct PSMoveSharpPointerState
    {
        public UInt32 valid;
        public float normalized_x;
        public float normalized_y;
        public void FillFromNetworkBuffer(ref Byte[] buffer, int i)
        {
            int offset = 1000 + i * 12;
            valid = NetworkReaderHelper.ReadUint32(ref buffer, offset);
            normalized_x = NetworkReaderHelper.ReadFloat(ref buffer, offset + 4);
            normalized_y = NetworkReaderHelper.ReadFloat(ref buffer, offset + 8);
        }
    }

    public class PSMoveSharpNavInfo
    {
        public const int PSMoveSharpNumNavControllers = 7;
        public PSMoveSharpNavInfo()
        {
            port_status = new UInt32[PSMoveSharpNumNavControllers];
        }

        public void FillFromNetworkBuffer(ref Byte[] buffer)
        {
            int offset = 1048;
            for (int i = 0; i < PSMoveSharpNumNavControllers; i++)
            {
                port_status[i] = NetworkReaderHelper.ReadUint32(ref buffer, offset);
                offset += 4;
            }
        }

        public UInt32[] port_status;
    }

    public class PSMoveSharpNavPadData
    {
        public PSMoveSharpNavPadData()
        {
            button = new UInt16[PSMoveSharpConstants.numButtons];
        }

        public void FillFromNetworkBuffer(ref Byte[] buffer, int i)
        {
            int offset = 1076 + i * 132;
            len = NetworkReaderHelper.ReadInt32(ref buffer, offset);
            for (int x = 0; x < PSMoveSharpConstants.numButtons; x++)
            {
                button[x] = NetworkReaderHelper.ReadUint16(ref buffer, offset + 4 + x * 2);
            }
        }
        public Int32 len;
        public UInt16[] button;
    }

    public class PSMoveSharpSphereState
    {
        public UInt32 tracking;
        public UInt32 tracking_hue;
        public float r;
        public float g;
        public float b;
        public void FillFromNetworkBuffer(ref Byte[] buffer, int i)
        {
            int offset = 2000 + i * 20;
            tracking = NetworkReaderHelper.ReadUint32(ref buffer, offset);
            tracking_hue = NetworkReaderHelper.ReadUint32(ref buffer, offset + 4);
            r = NetworkReaderHelper.ReadFloat(ref buffer, offset + 8);
            g = NetworkReaderHelper.ReadFloat(ref buffer, offset + 12);
            b = NetworkReaderHelper.ReadFloat(ref buffer, offset + 16);
        }
    }

    public class PSMoveSharpCameraState
    {
        public int exposure;
        public float exposure_time;
        public float gain;
        public float pitch_angle;
        public float pitch_angle_estimate;
        public void FillFromNetworkBuffer(ref Byte[] buffer)
        {
            UnityEngine.Debug.Log(buffer);
            int offset = 2080;
            exposure = NetworkReaderHelper.ReadInt32(ref buffer, offset);
            exposure_time = NetworkReaderHelper.ReadFloat(ref buffer, offset + 4);
            gain = NetworkReaderHelper.ReadFloat(ref buffer, offset + 8);
            pitch_angle = NetworkReaderHelper.ReadFloat(ref buffer, offset + 12);
            pitch_angle_estimate = NetworkReaderHelper.ReadFloat(ref buffer, offset + 16);
        }
    }

    public struct PSMoveSharpPositionPointerState
    {
        public UInt32 valid;
        public float normalized_x;
        public float normalized_y;
        public void FillFromNetworkBuffer(ref Byte[] buffer, int i)
        {
            int offset = 2100 + i * 12;
            valid = NetworkReaderHelper.ReadUint32(ref buffer, offset);
            normalized_x = NetworkReaderHelper.ReadFloat(ref buffer, offset + 4);
            normalized_y = NetworkReaderHelper.ReadFloat(ref buffer, offset + 8);
        }
    }
	
	public class PSMoveSharpCameraFrameSlice
    {
        public int index;
        public byte[] image;
        public int row_start;
        public int row_count;
        public PSMoveSharpCameraFrameSlice()
        {
            index = -1;
        }
    }
	
	
    public class PSMoveSharpCameraFrameStateCollector
    {
        public const int ImageWidth = 640;
        public const int ImageHeight = 480;

        protected int num_slices_;
        protected int current_index_;
        protected PSMoveSharpState last_state_;
        protected PSMoveSharpCameraFrameSlice[] slices_;

        /* Once we have a complete set of image slices
         * and a corresponding PSMoveSharpState, these
         * are set
         */
        protected PSMoveSharpState complete_state_;
        protected List<byte[]> complete_image_;

        public PSMoveSharpCameraFrameStateCollector()
        {
            num_slices_ = 1;
            current_index_ = 0;
            slices_ = new PSMoveSharpCameraFrameSlice[num_slices_];
            slices_[0] = new PSMoveSharpCameraFrameSlice();
            last_state_ = new PSMoveSharpState();
            complete_state_ = new PSMoveSharpState();
            complete_image_ = new List<byte[]>();
        }

        protected bool IsSetComplete()
        {
            // if all the image slices are from the most recent frame
            // we have a complete frame which can be made available
            // to the GUI
            bool completed = true;
            for (int i = 0; i < num_slices_; i++)
            {
                completed = completed && slices_[i].index == current_index_;
            }

            completed = completed && last_state_.packet_index == current_index_;

            return completed;
        }

        

        public void SetNumSlices(int num_slices)
        {
            num_slices_ = num_slices;
            slices_ = new PSMoveSharpCameraFrameSlice[num_slices_];
            for (int i = 0; i < num_slices; i++)
            {
                slices_[i] = new PSMoveSharpCameraFrameSlice();
            }
        }

        public void CaptureState(int index, PSMoveSharpState state)
        {
            if (index == current_index_)
            {
                last_state_ = state;
                if (IsSetComplete())
                {
                    complete_state_ = last_state_;
                    Console.WriteLine("Complete set packet index {0}", complete_state_.packet_index);
                    complete_image_ = MakeCompleteImage();
                }
            }
        }

        public void AddSlice(PSMoveSharpCameraFrameSlice slice)
        {
            if (slice.index > current_index_)
            {
                // always take the latest index
                current_index_ = slice.index;
            }
            int rows_per_slice = ImageHeight / num_slices_;
            int slice_index = slice.row_start / rows_per_slice;
            slices_[slice_index] = slice;
        }

        public List<byte[]> MakeCompleteImage()
        {
            List<byte[]> full_frame = new List<byte[]>();

            //Graphics gfx = Graphics.FromImage(full_frame);

            for (int i = 0; i < num_slices_; i++)
            {
                //TextureBrush brush = new TextureBrush(slices_[i].image);
                //gfx.FillRectangle(brush, 0, slices_[i].row_start, ImageWidth, slices_[i].row_count);
				full_frame.Add(slices_[i].image);
            }
			full_frame.Reverse();
			
            return full_frame;
        }

        public List<byte[]> GetCompleteImage()
        {
            return complete_image_;
        }

        public PSMoveSharpState GetCompleteState()
        {
            return complete_state_;
        }
    }

    public class PSMoveSharpCameraFrameState
    {
        public UInt32 packet_index;
        //protected List<byte[]> full_image;
        public const int ImageWidth = 640;
        public const int ImageHeight = 480;
        public System.Threading.ReaderWriterLock camera_frame_state_rwl;
        public PSMoveSharpCameraFrameStateCollector camera_frame_state_collector;

        public PSMoveSharpCameraFrameState()
        {
            camera_frame_state_rwl = new ReaderWriterLock();
            //full_image = new List<byte[]>();
			/*
            using (Graphics gfx = Graphics.FromImage(full_image))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 255, 255)))
            {
                gfx.FillRectangle(brush, 0, 0, ImageWidth, ImageHeight);
            }
            */
            camera_frame_state_collector = new PSMoveSharpCameraFrameStateCollector();
        }

        public void ProcessPacket(int index, byte[] image, int row_start, int row_count)
        {
            PSMoveSharpCameraFrameSlice slice = new PSMoveSharpCameraFrameSlice();
            slice.index = index;
            slice.image = image;
            slice.row_start = row_start;
            slice.row_count = row_count;
            camera_frame_state_rwl.AcquireWriterLock(-1);
            camera_frame_state_collector.AddSlice(slice);
            camera_frame_state_rwl.ReleaseWriterLock();
        }

        public void ProcessStatePacket(int index, PSMoveSharpState state)
        {
            camera_frame_state_rwl.AcquireWriterLock(-1);
            camera_frame_state_collector.CaptureState(index, state);
            camera_frame_state_rwl.ReleaseWriterLock();
        }

        public List<byte[]> GetCameraFrameAndState(ref PSMoveSharpState state)
        {
            List<byte[]> image;
            camera_frame_state_rwl.AcquireReaderLock(-1);
            state = camera_frame_state_collector.GetCompleteState();
            image = camera_frame_state_collector.GetCompleteImage();
            camera_frame_state_rwl.ReleaseReaderLock();
            return image;
        }
    }

    public class PSMoveSharpState
    {
        public const int PSMoveSharpNumMoveControllers = 4;
        
        public PSMoveSharpState()
        {
            serverConfig = new PSMoveSharpServerConfig();
            gemStatus = new PSMoveSharpStatus[PSMoveSharpNumMoveControllers];
            gemStates = new PSMoveSharpGemState[PSMoveSharpNumMoveControllers];
            imageStates = new PSMoveSharpImageState[PSMoveSharpNumMoveControllers];
            pointerStates = new PSMoveSharpPointerState[PSMoveSharpNumMoveControllers];
            sphereStates = new PSMoveSharpSphereState[PSMoveSharpNumMoveControllers];
            for (int i = 0; i < PSMoveSharpNumMoveControllers; i++)
            {
                sphereStates[i] = new PSMoveSharpSphereState();
            }
            cameraState = new PSMoveSharpCameraState();
            padData = new PSMoveSharpNavPadData[PSMoveSharpNavInfo.PSMoveSharpNumNavControllers];
            for (int i = 0; i < PSMoveSharpNavInfo.PSMoveSharpNumNavControllers; i++)
            {
                padData[i] = new PSMoveSharpNavPadData();
            }
            navInfo = new PSMoveSharpNavInfo();
            positionPointerStates = new PSMoveSharpPositionPointerState[PSMoveSharpNumMoveControllers];
            for (int i = 0; i < PSMoveSharpNumMoveControllers; i++)
            {
                positionPointerStates[i] = new PSMoveSharpPositionPointerState();
            }
        }
        public UInt32 packet_index;
        public PSMoveSharpServerConfig serverConfig;
        public PSMoveSharpStatus[] gemStatus;
        public PSMoveSharpGemState[] gemStates;
        public PSMoveSharpImageState[] imageStates;
        public PSMoveSharpPointerState[] pointerStates;
        public PSMoveSharpNavInfo navInfo;
        public PSMoveSharpNavPadData[] padData;
        public PSMoveSharpSphereState[] sphereStates;
        public PSMoveSharpCameraState cameraState;
        public PSMoveSharpPositionPointerState[] positionPointerStates;

    }

    public class PSMoveClient
    {		
        protected TcpClient _tcpClient;
        protected UdpClient _udpClient;

        public PSMoveClient()
        {
            _tcpClient = null;
            _udpClient = null;
        }

        public PSMoveSharpState ReadStateBlocking()
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            Byte[] buffer = _udpClient.Receive(ref remoteEP);
            PSMoveSharpState state = new PSMoveSharpState();
            state.packet_index = NetworkReaderHelper.ReadUint32(ref buffer, 12);
            state.serverConfig.FillFromNetworkBuffer(ref buffer);
            for (int i = 0; i < PSMoveSharpState.PSMoveSharpNumMoveControllers; i++)
            {
                state.gemStatus[i].FillFromNetworkBuffer(ref buffer, i);
                state.gemStates[i].FillFromNetworkBuffer(ref buffer, i);
                state.imageStates[i].FillFromNetworkBuffer(ref buffer, i);
                state.pointerStates[i].FillFromNetworkBuffer(ref buffer, i);
                state.sphereStates[i].FillFromNetworkBuffer(ref buffer, i);
                state.positionPointerStates[i].FillFromNetworkBuffer(ref buffer, i);
            }
            state.cameraState.FillFromNetworkBuffer(ref buffer);
            state.navInfo.FillFromNetworkBuffer(ref buffer);
            for (int i = 0; i < PSMoveSharpNavInfo.PSMoveSharpNumNavControllers; i++)
            {
                state.padData[i].FillFromNetworkBuffer(ref buffer, i);
            }
            return state;
        }

        public enum ClientRequest
        {
            PSMoveClientRequestInit =                       0x0,
            PSMoveClientRequestPause =                      0x1,
            PSMoveClientRequestResume =                     0x2,
            PSMoveClientRequestDelayChange =                0x3,
            PSMoveClientRequestPrepareCamera =              0x4,
            PSMoveClientRequestCalibrateController =        0x5,
            PSMoveClientRequestPointerSetLeft =             0x7,
            PSMoveClientRequestPointerSetRight =            0x8,
            PSMoveClientRequestPointerSetBottom =           0x9,
            PSMoveClientRequestPointerSetTop =              0x10,
            PSMoveClientRequestPointerEnable =              0x11,
            PSMoveClientRequestPointerDisable =             0x12,
            PSMoveClientRequestControllerReset =            0x13,
            PSMoveClientRequestPositionPointerSetLeft =	    0x14,
            PSMoveClientRequestPositionPointerSetRight =	0x15,
            PSMoveClientRequestPositionPointerSetBottom =	0x16,
            PSMoveClientRequestPositionPointerSetTop =	    0x17,
            PSMoveClientRequestPositionPointerEnable =	    0x18,
            PSMoveClientRequestPositionPointerDisable =	    0x19,
            PSMoveClientRequestForceRGB =                   0x20,
            PSMoveClientRequestSetRumble =                  0x21,
            PSMoveClientRequestTrackHues =                  0x22,
            PSMoveClientRequestCameraFrameDelayChange =     0x23,
            PSMoveClientRequestCameraFrameSetNumSlices =    0x24,
            PSMoveClientRequestCameraFramePause =           0x25,
            PSMoveClientRequestCameraFrameResume =          0x26,
        }

        public void SendRequestPacket(ClientRequest request, uint payload)
        {
            byte[] PacketData = new byte[12];
            int request_net = System.Net.IPAddress.HostToNetworkOrder((int)request);
            int payload_size_net = System.Net.IPAddress.HostToNetworkOrder(4);
            int payload_net = System.Net.IPAddress.HostToNetworkOrder((int)payload);
            Buffer.BlockCopy(BitConverter.GetBytes(request_net), 0, PacketData, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(payload_size_net), 0, PacketData, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(payload_net), 0, PacketData, 8, 4);
            NetworkStream stream = _tcpClient.GetStream();
            stream.Write(PacketData, 0, PacketData.Length);
        }

        public void SendRequestPacket(ClientRequest request, uint gem_num, uint rumble) {
            byte[] PacketData = new byte[16];
            int request_net = System.Net.IPAddress.HostToNetworkOrder((int)request);
            int payload_size_net = System.Net.IPAddress.HostToNetworkOrder(8);
            int gem_num_net = System.Net.IPAddress.HostToNetworkOrder((int)gem_num);
            int rumble_net = System.Net.IPAddress.HostToNetworkOrder((int)rumble);
            Buffer.BlockCopy(BitConverter.GetBytes(request_net), 0, PacketData, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(payload_size_net), 0, PacketData, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(gem_num_net), 0, PacketData, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(rumble_net), 0, PacketData, 12, 4);
            NetworkStream stream = _tcpClient.GetStream();
            stream.Write(PacketData, 0, PacketData.Length);
        }

        public void SendRequestPacket(ClientRequest request, uint gem_num, float r, float g, float b) {
            byte[] PacketData = new byte[24];
            int request_net = System.Net.IPAddress.HostToNetworkOrder((int)request);
            int payload_size_net = System.Net.IPAddress.HostToNetworkOrder(16);
            int gem_num_net = System.Net.IPAddress.HostToNetworkOrder((int)gem_num);
            Buffer.BlockCopy(BitConverter.GetBytes(request_net), 0, PacketData, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(payload_size_net), 0, PacketData, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(gem_num_net), 0, PacketData, 8, 4);
            NetworkWriterHelper.WriteFloat(r, ref PacketData, 12);
            NetworkWriterHelper.WriteFloat(g, ref PacketData, 16);
            NetworkWriterHelper.WriteFloat(b, ref PacketData, 20);
            NetworkStream stream = _tcpClient.GetStream();
            stream.Write(PacketData, 0, PacketData.Length);
        }

        public void SendRequestPacket(ClientRequest request, uint req_hue_0, uint req_hue_1, uint req_hue_2, uint req_hue_3) {
            byte[] PacketData = new byte[24];
            int request_net = System.Net.IPAddress.HostToNetworkOrder((int)request);
            int payload_size_net = System.Net.IPAddress.HostToNetworkOrder(16);
            int req_hue_0_net = System.Net.IPAddress.HostToNetworkOrder((int)req_hue_0);
            int req_hue_1_net = System.Net.IPAddress.HostToNetworkOrder((int)req_hue_1);
            int req_hue_2_net = System.Net.IPAddress.HostToNetworkOrder((int)req_hue_2);
            int req_hue_3_net = System.Net.IPAddress.HostToNetworkOrder((int)req_hue_3);

            Buffer.BlockCopy(BitConverter.GetBytes(request_net), 0, PacketData, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(payload_size_net), 0, PacketData, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(req_hue_0_net), 0, PacketData, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(req_hue_1_net), 0, PacketData, 12, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(req_hue_2_net), 0, PacketData, 16, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(req_hue_3_net), 0, PacketData, 20, 4);
            NetworkStream stream = _tcpClient.GetStream();
            stream.Write(PacketData, 0, PacketData.Length);
        }

        public void Connect(String server, int port)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(server, port);
            _udpClient = new UdpClient(0);
            Console.WriteLine("Initial recieve buffer size: {0}", _udpClient.Client.ReceiveBufferSize);
            _udpClient.Client.ReceiveBufferSize = 655360; // 640 KB
            Console.WriteLine("Expanded recieve buffer size: {0}", _udpClient.Client.ReceiveBufferSize);
            uint udpport = (uint)((IPEndPoint)_udpClient.Client.LocalEndPoint).Port;
            SendRequestPacket(ClientRequest.PSMoveClientRequestInit, udpport);
        }

        public void Pause()
        {
            SendRequestPacket(ClientRequest.PSMoveClientRequestPause, 0);
        }

        public void ForceRGB(int gem_num, float r, float g, float b)
        {
            SendRequestPacket(ClientRequest.PSMoveClientRequestForceRGB, (uint)gem_num, r, g, b);
        }
        public void Resume()
        {
            SendRequestPacket(ClientRequest.PSMoveClientRequestResume, 0);
        }

        public void DelayChange(uint delay_ms)
        {
            SendRequestPacket(ClientRequest.PSMoveClientRequestDelayChange, delay_ms);
        }

        public void CameraFrameDelayChange(uint image_delay_ms)
        {
            SendRequestPacket(ClientRequest.PSMoveClientRequestCameraFrameDelayChange, image_delay_ms);
        }

        public void CameraFrameSetNumSlices(uint num_slices)
        {
            SendRequestPacket(ClientRequest.PSMoveClientRequestCameraFrameSetNumSlices, num_slices);
        }

        public void CameraFramePause()
        {
            SendRequestPacket(ClientRequest.PSMoveClientRequestCameraFramePause, 0);
        }

        public void CameraFrameResume()
        {
            SendRequestPacket(ClientRequest.PSMoveClientRequestCameraFrameResume, 0);
        }

        public void CalibrateController(int controller)
        {
            SendRequestPacket(ClientRequest.PSMoveClientRequestCalibrateController, (uint)controller);
        }

        public void TrackAllHues()
        {
            const uint PICK_FOR_ME = (4<<24);
            //const uint DONT_TRACK = (2<<24);
            SendRequestPacket(ClientRequest.PSMoveClientRequestTrackHues, PICK_FOR_ME, PICK_FOR_ME, PICK_FOR_ME, PICK_FOR_ME);
        }

        public void Close()
        {
            _tcpClient.Close();
            _udpClient.Close();
        }
    }

    public class PSMoveClientThreadedRead : PSMoveClient
    {
        protected PSMoveSharpState _latest_state;
        protected PSMoveSharpCameraFrameState _latest_camera_frame_state;

        protected Thread _readerThread;
        protected uint _readerThreadExit;
        protected bool _reading;
        protected ReaderWriterLock _rwl;

        protected class UdpState
        {
            public IPEndPoint e;
            public UdpClient u;
        }

        protected void ReceiveCallback(IAsyncResult ar)
        {
            UdpClient u = (UdpClient)((UdpState)(ar.AsyncState)).u;
            IPEndPoint e = (IPEndPoint)((UdpState)(ar.AsyncState)).e;
            Byte[] buffer = null;
            
            try
            {
                buffer = u.EndReceive(ar, ref e);
            }
            catch (System.ObjectDisposedException)
            {
                _reading = false;
                return;
            }
            catch (System.Net.Sockets.SocketException)
            {
                _reading = false;
                return;
            }
            _rwl.AcquireWriterLock(-1);
            
            int packet_length = NetworkReaderHelper.ReadInt32(ref buffer, 12);
            //int magic = NetworkReaderHelper.ReadInt32(ref buffer, 0);
            int code = NetworkReaderHelper.ReadInt32(ref buffer, 8);
            uint packet_index = NetworkReaderHelper.ReadUint32(ref buffer, 16);
			
			//UnityEngine.Debug.Log(code);
            if (code == 1) {
                _latest_state.packet_index = NetworkReaderHelper.ReadUint32(ref buffer, 16);
                _latest_state.serverConfig.FillFromNetworkBuffer(ref buffer);
                for (int i = 0; i < PSMoveSharpState.PSMoveSharpNumMoveControllers; i++)
                {
                    _latest_state.gemStatus[i].FillFromNetworkBuffer(ref buffer, i);
                    _latest_state.gemStates[i].FillFromNetworkBuffer(ref buffer, i);
                    _latest_state.imageStates[i].FillFromNetworkBuffer(ref buffer, i);
                    _latest_state.pointerStates[i].FillFromNetworkBuffer(ref buffer, i);
                    _latest_state.sphereStates[i].FillFromNetworkBuffer(ref buffer, i);
                    _latest_state.positionPointerStates[i].FillFromNetworkBuffer(ref buffer, i);
                }
                _latest_state.navInfo.FillFromNetworkBuffer(ref buffer);
                for (int i = 0; i < PSMoveSharpNavInfo.PSMoveSharpNumNavControllers; i++)
                {
                    _latest_state.padData[i].FillFromNetworkBuffer(ref buffer, i);
                }

            } else if (code == 2) {
                byte[] slice = new byte[packet_length - 3];
                int slice_num = NetworkReaderHelper.ReadByte(ref buffer, 20);
                int num_slices = NetworkReaderHelper.ReadByte(ref buffer, 21);
                //int format = NetworkReaderHelper.ReadByte(ref buffer, 22);
                int row_height = 480 / num_slices;
                int row_start = row_height * slice_num;
                Array.Copy(buffer, 23, slice, 0, packet_length - 3);
				/*
                System.IO.MemoryStream jpeg_stream = new System.IO.MemoryStream(jpeg_data);
                System.Drawing.Image slice = null;
                try
                {
                    slice = Image.FromStream(jpeg_stream);
					testByte = jpeg_data;
                }
                catch
                {
                    // server sent a bad frame
                }
                */
                if (slice != null)
                {
                    _latest_camera_frame_state.ProcessPacket((int)packet_index, slice, row_start, row_height);
                    //_latest_camera_frame_state.BlitSliceOntoFullImage(slice, row_start, row_height);
                }
            } else if (code == 3) {
                PSMoveSharpState camera_state = new PSMoveSharpState();
                camera_state.packet_index = NetworkReaderHelper.ReadUint32(ref buffer, 16);
                camera_state.serverConfig.FillFromNetworkBuffer(ref buffer);
                for (int i = 0; i < PSMoveSharpState.PSMoveSharpNumMoveControllers; i++)
                {
                    camera_state.gemStatus[i].FillFromNetworkBuffer(ref buffer, i);
                    camera_state.gemStates[i].FillFromNetworkBuffer(ref buffer, i);
                    camera_state.imageStates[i].FillFromNetworkBuffer(ref buffer, i);
                    camera_state.pointerStates[i].FillFromNetworkBuffer(ref buffer, i);
                    camera_state.sphereStates[i].FillFromNetworkBuffer(ref buffer, i);
                    camera_state.positionPointerStates[i].FillFromNetworkBuffer(ref buffer, i);
                }
                camera_state.navInfo.FillFromNetworkBuffer(ref buffer);
                for (int i = 0; i < PSMoveSharpNavInfo.PSMoveSharpNumNavControllers; i++)
                {
                    camera_state.padData[i].FillFromNetworkBuffer(ref buffer, i);
                }

                _latest_camera_frame_state.ProcessStatePacket((int)packet_index, camera_state);
            }
            
            _rwl.ReleaseWriterLock();
            _reading = false;
        }

        protected void PSMoveClientThreadedReadThreadStart()
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            UdpState s = new UdpState();
            while (_readerThreadExit == 0)
            {
                s.e = remoteEP;
                s.u = _udpClient;
                _reading = true;
                try
                {
                    if (_udpClient != null)
                        _udpClient.BeginReceive(new AsyncCallback(ReceiveCallback), s);
                } catch (System.Exception)
                {
                    // socket closed exception
                    Console.WriteLine("except from udp client begin receive");
                }
                while (_reading)
                {
                    if (_readerThreadExit == 1)
                    {
                        break;
                    }
                    Thread.Sleep(0);
                }
            }
        }

        public PSMoveClientThreadedRead()
        {
            _readerThread = new Thread(new ThreadStart(PSMoveClientThreadedReadThreadStart));
            _rwl = new ReaderWriterLock();
            _readerThreadExit = 0;
            _latest_state = new PSMoveSharpState();
            _latest_camera_frame_state = new PSMoveSharpCameraFrameState();
            _reading = false;
        }

        ~PSMoveClientThreadedRead()
        {
            _readerThreadExit = 1;

            try
            {
                _readerThread.Join();
            }
            catch (System.Exception)
            {
            	
            }            
        }

        public void StartThread()
        {
            _readerThread.Start();
        }

        public void StopThread ()
        {
            _readerThreadExit = 1;
            _readerThread.Join();
        }

        public PSMoveSharpState GetLatestState ()
        {
            PSMoveSharpState state;
            _rwl.AcquireReaderLock(-1);
            state = _latest_state;
            _rwl.ReleaseReaderLock();
            return state;
        }

        public PSMoveSharpState GetLatestStateForCameraFrame()
        {
            PSMoveSharpState state;
            _rwl.AcquireReaderLock(-1);
            state = _latest_camera_frame_state.camera_frame_state_collector.GetCompleteState();
            _rwl.ReleaseReaderLock();
            return state;
        }

        public PSMoveSharpCameraFrameState GetLatestCameraFrameState()
        {
            PSMoveSharpCameraFrameState camera_frame_state;
            _rwl.AcquireReaderLock(-1);
            camera_frame_state = _latest_camera_frame_state;
            _rwl.ReleaseReaderLock();
            return camera_frame_state;
        }

        public void SetNumImageSlices(int num_slices)
        {
            _rwl.AcquireWriterLock(-1);
            _latest_camera_frame_state.camera_frame_state_collector.SetNumSlices(num_slices);
            _rwl.ReleaseWriterLock();
        }
    }
}
