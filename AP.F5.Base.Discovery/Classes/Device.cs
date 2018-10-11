using Lextm.SharpSnmpLib;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using System;
using System.Collections.Generic;

namespace AP.F5.Base.Discovery.Classes
{
    /// <summary>
    /// Simple Seed Device Used for Initial DeviceGroup Detection
    /// </summary>
    public class f5Device
    {
        // Properties (From CSV File)
        public string Address { get; set; }         // IP Address of Device
        public string Community { get; set; }       // SNMP community string to query with
        public int Port { get; set; }               // SNMP Port to query with
        public string F5usr { get; set; }           // f5 UserName for Device
        public string F5pwd { get; set; }           // F5 Password for Device

        // Properties (From SNMP)
        public string HwName { get; set; }          // Device Hardware Name (Determines Physical or Virtual)
        public string Type { get; set; }            // Type of Device (Physical or Virtual)
        public string SystemNodeName { get; set; }  // Name of Device
        public string Model;                        // Device Model Number
        public string SystemOID;                    // System OID
        public string Location;                     // System Location
        public string Contact;                      // System Contact
        public string SoftwareVersion;              // Software Info.
        public string SoftwareBuild;                // Software Info.
        public string SoftwareHotFix;               // Software Info.
        public bool Standalone;                     // Is The Device Standalone
        public string SerialNumber;                 // Serial Number of Device
        public string Active;                       // Active Status
        public string ActiveTrafficGroup;           // ActiveTrafficGroup

        // SCOM Management Group
        private static ManagementGroup m_managementGroup;

        // Device SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object_Device;
        
        // Failover State SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object_FailoverState;

        // Memory SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object_Memory;

        /// <summary>
        /// List of Fans
        /// </summary>
        public List<Fan> Fans = new List<Fan>();
        // Fan Group SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object_FanGroup;

        /// <summary>
        /// List of PowerSupplies
        /// </summary>
        public List<PowerSupply> PowerSupplies = new List<PowerSupply>();
        // Power Supplies Group SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object_PowerSupplyGroup;

        /// <summary>
        /// List of Processors
        /// </summary>
        public List<Processor> Processors = new List<Processor>();
        // Processor Group SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object_ProcessorGroup;

        /// <summary>
        /// List of Temp. Sensors
        /// </summary>
        public List<TempSensor> TempSensors = new List<TempSensor>();
        // Temp Sensors Group SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object_TempSensorsGroup;

        /// <summary>
        /// List of DiskPartitions
        /// </summary>
        public List<DiskPartition> DiskPartitions = new List<DiskPartition>();
        // Fan Group SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object_DiskPartitionGroup;


        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="address"></param>
        /// <param name="community"></param>
        /// <param name="port"></param>
        /// <param name="f5usr"></param>
        /// <param name="f5pwd"></param>
        public f5Device(ManagementGroup mg, string address, string community, int port, string f5usr, string f5pwd)
        {
            // Properties from Arguments
            Address = address;
            Community = community;
            Port = port;
            F5usr = f5usr;
            F5pwd = f5pwd;
            m_managementGroup = mg;

            // Get System OID
            SystemOID = SNMP.GetSNMP(SNMP.sysObjectID, address, port, community)[0].Data.ToString();

            // Get SysSystem Info
            SystemNodeName = SNMP.GetSNMP(SNMP.sysSystemName, address, port, community)[0].Data.ToString();

            // Find Out if Virtual Or Physical
            HwName = SNMP.GetSNMP(SNMP.sysGeneralHwName, address, port, community)[0].Data.ToString();
            if (HwName == "Z100") { Type = "Virtual"; } else { Type = "Physical"; }

            // Get Model
            Model = SNMP.GetSNMP(SNMP.sysPlatformInfoMarketingName, address, port, community)[0].Data.ToString();

            // Get SerialNumber
            SerialNumber = SNMP.GetSNMP(SNMP.sysGeneralChassisSerialNum, address, port, community)[0].Data.ToString();

            // Get Location
            Location = SNMP.GetSNMP(SNMP.sysLocation, address, port, community)[0].Data.ToString();

            // Get Contact
            Contact = SNMP.GetSNMP(SNMP.sysContact, address, port, community)[0].Data.ToString();

            // Get Product Info
            List<Variable> returnlist = SNMP.BulkSNMP(SNMP.sysProduct, address, port, community);
            SoftwareVersion = returnlist[1].Data.ToString();
            SoftwareBuild = returnlist[2].Data.ToString();
            SoftwareHotFix = returnlist[3].Data.ToString();

            // Get Standalone
            int iSynStatus = Convert.ToInt32(SNMP.GetSNMP(SNMP.sysCmSyncStatusId, address, port, community)[0].Data.ToString());
            if (iSynStatus==6)
            {
                Standalone = true;
            } else
            {
                Standalone = false;
            }

            // Create Scom Object
            CreateDeviceScomObject();

            // Create Failover State Object And Relationship
            CreateFailoverStateScomObjects();

            CreateMemoryScomObjects();

            // Create

            // Get Fans (if Virtual will return 0 Fans)
            GetFans();

            // Get Power Supplies (if Virtual will return 0 Fans)
            GetPowerSupplies();

            // Get Temp Sensors (if Virtual will return 0 Fans)
            GetTempSensors();

            // Get Processors
            GetProcessors();

            // Get Disk Partitions
            GetDiskPartitions();

        }

        /// <summary>
        /// Create Scom Object
        /// </summary>
        private void CreateDeviceScomObject()
        {
            // Create Root Entity Class & Display Name Prop
            ManagementPackClass mpc_Entity = SCOM_Functions.GetManagementPackClass("System.Entity");
            ManagementPackProperty mpp_EntityDisplayName = mpc_Entity.PropertyCollection["DisplayName"];

            // Create Device Group Management Pack Class
            ManagementPackClass mpc_Device = SCOM_Functions.GetManagementPackClass("AP.F5.Device");

            // Create New Device Group Object
            SCOM_Object_Device = new CreatableEnterpriseManagementObject(m_managementGroup, mpc_Device);
            // Display Name of Parent (KEY Property)
            SCOM_Object_Device[mpp_EntityDisplayName].Value = SystemNodeName;

            // Set Properties of Device
            // IPAddress 
            ManagementPackProperty mpp_DeviceAddress = mpc_Device.PropertyCollection["IPAddress"];
            SCOM_Object_Device[mpp_DeviceAddress].Value = Address;
            // Name (Key Property)
            ManagementPackProperty mpp_DeviceName = mpc_Device.PropertyCollection["DeviceName"];
            SCOM_Object_Device[mpp_DeviceName].Value = SystemNodeName;
            // Type
            ManagementPackProperty mpp_DeviceType = mpc_Device.PropertyCollection["Type"];
            SCOM_Object_Device[mpp_DeviceType].Value = Type;
            // Port
            ManagementPackProperty mpp_DevicePort = mpc_Device.PropertyCollection["Port"];
            SCOM_Object_Device[mpp_DevicePort].Value = Port;
            // Community
            ManagementPackProperty mpp_DeviceCommunity = mpc_Device.PropertyCollection["Community"];
            SCOM_Object_Device[mpp_DeviceCommunity].Value = Community;
            // SystemOID
            ManagementPackProperty mpp_DeviceSystemOID = mpc_Device.PropertyCollection["SystemOID"];
            SCOM_Object_Device[mpp_DeviceSystemOID].Value = SystemOID;
            // Location
            ManagementPackProperty mpp_DeviceLocation = mpc_Device.PropertyCollection["Location"];
            SCOM_Object_Device[mpp_DeviceLocation].Value = Location;
            // Contact
            ManagementPackProperty mpp_DeviceContact = mpc_Device.PropertyCollection["Contact"];
            SCOM_Object_Device[mpp_DeviceContact].Value = Contact;
            // Model
            ManagementPackProperty mpp_DeviceModel = mpc_Device.PropertyCollection["Model"];
            SCOM_Object_Device[mpp_DeviceModel].Value = Model;
            // Vendor
            ManagementPackProperty mpp_DeviceVendor = mpc_Device.PropertyCollection["Vendor"];
            SCOM_Object_Device[mpp_DeviceVendor].Value = "F5 Networks, Inc.";
            // Version
            ManagementPackProperty mpp_DeviceVersion = mpc_Device.PropertyCollection["Version"];
            SCOM_Object_Device[mpp_DeviceVersion].Value = SoftwareVersion;
            // Build
            ManagementPackProperty mpp_DeviceBuild = mpc_Device.PropertyCollection["Build"];
            SCOM_Object_Device[mpp_DeviceBuild].Value = SoftwareBuild;
            // HotFix
            ManagementPackProperty mpp_DeviceHotFix = mpc_Device.PropertyCollection["HotFix"];
            SCOM_Object_Device[mpp_DeviceHotFix].Value = SoftwareHotFix;
            // SerialNumber
            ManagementPackProperty mpp_DeviceSerialNumber = mpc_Device.PropertyCollection["SerialNumber"];
            SCOM_Object_Device[mpp_DeviceSerialNumber].Value = SerialNumber;
            // Standalone
            ManagementPackProperty mpp_DeviceStandalone = mpc_Device.PropertyCollection["Standalone"];
            SCOM_Object_Device[mpp_DeviceStandalone].Value = Standalone;

        }

        /// <summary>
        /// Create FailoverState SCOM Object and Relationship Object
        /// </summary>
        private void CreateFailoverStateScomObjects()
        {
            // Create Device Group Management Pack Class
            ManagementPackClass mpc_Device = SCOM_Functions.GetManagementPackClass("AP.F5.Device");

            // Parent Device Key Property (IP Address)
            ManagementPackProperty mpp_DeviceKey = mpc_Device.PropertyCollection["DeviceName"];

            // Create FailoverState Object
            ManagementPackClass mpc_DeviceFailoverState = SCOM_Functions.GetManagementPackClass("AP.F5.Device.FailoverState");
            SCOM_Object_FailoverState = new CreatableEnterpriseManagementObject(m_managementGroup, mpc_DeviceFailoverState);
            SCOM_Object_FailoverState[mpp_DeviceKey].Value = SystemNodeName; // Set Key of Device
            ManagementPackProperty mpp_DeviceFailoverStateName = mpc_DeviceFailoverState.PropertyCollection["Name"];
            SCOM_Object_FailoverState[mpp_DeviceFailoverStateName].Value = "Failover State";

        }

        /// <summary>
        /// Create Memory SCOM Object and Relationship Object
        /// </summary>
        private void CreateMemoryScomObjects()
        {
            // Create Device Group Management Pack Class
            ManagementPackClass mpc_Device = SCOM_Functions.GetManagementPackClass("AP.F5.Device");

            // Parent Device Key Property (IP Address)
            ManagementPackProperty mpp_DeviceKey = mpc_Device.PropertyCollection["DeviceName"];

            // Create FailoverState Object
            ManagementPackClass mpc_DeviceMemory = SCOM_Functions.GetManagementPackClass("AP.F5.Device.Memory");
            SCOM_Object_Memory = new CreatableEnterpriseManagementObject(m_managementGroup, mpc_DeviceMemory);
            SCOM_Object_Memory[mpp_DeviceKey].Value = SystemNodeName; // Set Key of Device
            ManagementPackProperty mpp_DeviceMemoryName = mpc_DeviceMemory.PropertyCollection["Name"];
            SCOM_Object_Memory[mpp_DeviceMemoryName].Value = "Memory";

        }

        /// <summary>
        /// Get Fans
        /// </summary>
        private void GetFans()
        {
            // Get Count of Fans
            int fanCount = Convert.ToInt32(SNMP.GetSNMP(SNMP.sysChassisFanNumber, Address, Port, Community)[0].Data.ToString());

            // If There Are Fans Then Create FanGroup and Relationship
            if (fanCount > 0)
            {
                // Create Device Group Management Pack Class
                ManagementPackClass mpc_Device = SCOM_Functions.GetManagementPackClass("AP.F5.Device");

                // Create Root Entity Class & Display Name Prop
                ManagementPackClass mpc_Entity = SCOM_Functions.GetManagementPackClass("System.Entity");
                ManagementPackProperty mpp_EntityDisplayName = mpc_Entity.PropertyCollection["DisplayName"];

                // Parent Device Key Property (IP Address)
                ManagementPackProperty mpp_DeviceKey = mpc_Device.PropertyCollection["DeviceName"];

                // Create FanGroup Object
                ManagementPackClass mpc_DeviceFanGroup = SCOM_Functions.GetManagementPackClass("AP.F5.Device.FansGroup");
                SCOM_Object_FanGroup = new CreatableEnterpriseManagementObject(m_managementGroup, mpc_DeviceFanGroup);
                SCOM_Object_FanGroup[mpp_DeviceKey].Value = SystemNodeName; // Set Key of Device
                ManagementPackProperty mpp_DeviceFansGroupName = mpc_DeviceFanGroup.PropertyCollection["Name"];
                SCOM_Object_FanGroup[mpp_DeviceFansGroupName].Value = "Fans";


                // Create Management Pack Class Objects for Fan and Needed Properties
                ManagementPackClass mpc_Fan = SCOM_Functions.GetManagementPackClass(className: "AP.F5.Device.Fan");
                ManagementPackProperty mpp_FanIndex = mpc_Fan.PropertyCollection["Index"];
                ManagementPackProperty mpp_FanName = mpc_Fan.PropertyCollection["Name"];

                // Set Fan-Group Relationship
                ManagementPackRelationship mpr_FanGroup = SCOM_Functions.GetManagementPackRelationship("AP.F5.Device.FansGroup.HostsFans");

                // Create Fans
                for (int i = 1; i <= fanCount; i++)
                {
                    Fan newfan = new Fan();

                    // Create SCOM Fan Object
                    newfan.SCOM_Object_Fan = new CreatableEnterpriseManagementObject(SCOM_Functions.m_managementGroup, mpc_Fan);
                    // Set Key of Device
                    newfan.SCOM_Object_Fan[mpp_DeviceKey].Value = SystemNodeName;
                    // Set Key of Fans Group
                    newfan.SCOM_Object_Fan[mpp_DeviceFansGroupName].Value = "Fans";
                    //Set Logical Entity Display Name
                    newfan.SCOM_Object_Fan[mpp_EntityDisplayName].Value = "Fan-" + i.ToString();
                    // Set Fan Properties
                    newfan.SCOM_Object_Fan[mpp_FanIndex].Value = Convert.ToInt32(SNMP.GetSNMP(SNMP.sysChassisFanIndex + i.ToString(), Address, Port, Community)[0].Data.ToString()); ;
                    newfan.SCOM_Object_Fan[mpp_FanName].Value = "Fan-" + i.ToString();

                    Fans.Add(newfan);
                }
            }
        }

        /// <summary>
        /// Get Processors
        /// </summary>
        private void GetProcessors()
        {
            // Get
            var processors = SNMP.WalkSNMP(SNMP.sysMultiHostCpuIndex, Address, Port, Community);

            if (processors.Count > 0)
            {
                // Create Device Management Pack Class
                ManagementPackClass mpc_Device = SCOM_Functions.GetManagementPackClass("AP.F5.Device");

                // Create Root Entity Class & Display Name Prop
                ManagementPackClass mpc_Entity = SCOM_Functions.GetManagementPackClass("System.Entity");
                ManagementPackProperty mpp_EntityDisplayName = mpc_Entity.PropertyCollection["DisplayName"];

                // Parent Device Key Property (IP Address)
                ManagementPackProperty mpp_DeviceKey = mpc_Device.PropertyCollection["DeviceName"];

                // Create Processors Group Object
                ManagementPackClass mpc_DeviceProcessorsGroup = SCOM_Functions.GetManagementPackClass("AP.F5.Device.ProcessorsGroup");
                SCOM_Object_ProcessorGroup = new CreatableEnterpriseManagementObject(m_managementGroup, mpc_DeviceProcessorsGroup);
                SCOM_Object_ProcessorGroup[mpp_DeviceKey].Value = SystemNodeName; // Set Key of Device
                ManagementPackProperty mpp_DeviceProcessorsGroupName = mpc_DeviceProcessorsGroup.PropertyCollection["Name"];
                SCOM_Object_ProcessorGroup[mpp_DeviceProcessorsGroupName].Value = "Processors";


                // Create Management Pack Class Objects for Processor and Needed Properties
                ManagementPackClass mpc_Processor = SCOM_Functions.GetManagementPackClass(className: "AP.F5.Device.Processor");
                ManagementPackProperty mpp_ProcessorIndex = mpc_Processor.PropertyCollection["Index"];
                ManagementPackProperty mpp_ProcessorName = mpc_Processor.PropertyCollection["Name"];

                // Set Processor-Group Relationship
                ManagementPackRelationship mpr_ProcessorsGroup = SCOM_Functions.GetManagementPackRelationship("AP.F5.Device.ProcessorsGroup.HostsProcessors");


                for (int i = 0; i < processors.Count; i++)
                {
                    Processor newProcessor = new Processor();
                    string index = "." + processors[i].Id.ToString();
                    index = index.Replace(SNMP.sysMultiHostCpuIndex, "");

                    // Create SCOM Fan Object
                    newProcessor.SCOM_Object_Processor= new CreatableEnterpriseManagementObject(SCOM_Functions.m_managementGroup, mpc_Processor);
                    // Set Key of Device
                    newProcessor.SCOM_Object_Processor[mpp_DeviceKey].Value = SystemNodeName;
                    // Set Key of Fans Group
                    newProcessor.SCOM_Object_Processor[mpp_DeviceProcessorsGroupName].Value = "Processors";
                    //Set Logical Entity Display Name
                    newProcessor.SCOM_Object_Processor[mpp_EntityDisplayName].Value = "CPU-" + i.ToString();
                    // Set Fan Properties
                    newProcessor.SCOM_Object_Processor[mpp_ProcessorIndex].Value = index;
                    newProcessor.SCOM_Object_Processor[mpp_ProcessorName].Value = "CPU-" + i.ToString();

                    Processors.Add(newProcessor);
                }

            }
        }

        /// <summary>
        /// Get Power Supplies
        /// </summary>
        private void GetPowerSupplies()
        {
            // Get Count of PowerSupplies
            int powerSupplyCount = Convert.ToInt16(SNMP.GetSNMP(SNMP.sysChassisPowerSupplyNumber, Address, Port, Community)[0].Data.ToString());

            // If There are Fans Then Continue
            if (powerSupplyCount > 0)
            {
                // Create Device Group Management Pack Class
                ManagementPackClass mpc_Device = SCOM_Functions.GetManagementPackClass("AP.F5.Device");

                // Create Root Entity Class & Display Name Prop
                ManagementPackClass mpc_Entity = SCOM_Functions.GetManagementPackClass("System.Entity");
                ManagementPackProperty mpp_EntityDisplayName = mpc_Entity.PropertyCollection["DisplayName"];

                // Parent Device Key Property (IP Address)
                ManagementPackProperty mpp_DeviceKey = mpc_Device.PropertyCollection["DeviceName"];

                // Create PowerSuppliesGroup Object
                ManagementPackClass mpc_DevicePowerSupplyGroup = SCOM_Functions.GetManagementPackClass("AP.F5.Device.PowerSuppliesGroup");
                SCOM_Object_PowerSupplyGroup = new CreatableEnterpriseManagementObject(m_managementGroup, mpc_DevicePowerSupplyGroup);
                SCOM_Object_PowerSupplyGroup[mpp_DeviceKey].Value = SystemNodeName; // Set Key of Device
                ManagementPackProperty mpp_DevicePowerSuppliesGroupName = mpc_DevicePowerSupplyGroup.PropertyCollection["Name"];
                SCOM_Object_PowerSupplyGroup[mpp_DevicePowerSuppliesGroupName].Value = "PSUs";

                // Create Management Pack Class Objects for PowerSupply and Needed Properties
                ManagementPackClass mpc_PowerSupply = SCOM_Functions.GetManagementPackClass(className: "AP.F5.Device.PowerSupply");
                ManagementPackProperty mpp_PowerSupplyIndex = mpc_PowerSupply.PropertyCollection["Index"];
                ManagementPackProperty mpp_PowerSupplyName = mpc_PowerSupply.PropertyCollection["Name"];

                // Set PowerSupply-Group Relationship
                ManagementPackRelationship mpr_PowerSuppliesGroup = SCOM_Functions.GetManagementPackRelationship("AP.F5.Device.PowerSuppliesGroup.HostsPowerSupplies");


                for (int i = 1; i <= powerSupplyCount; i++)
                {
                    PowerSupply newpsu = new PowerSupply();

                    // Create SCOM  Object
                    newpsu.SCOM_Object_PowerSupply = new CreatableEnterpriseManagementObject(SCOM_Functions.m_managementGroup, mpc_PowerSupply);
                    // Set Key of Device
                    newpsu.SCOM_Object_PowerSupply[mpp_DeviceKey].Value = SystemNodeName;
                    // Set Key of Fans Group
                    newpsu.SCOM_Object_PowerSupply[mpp_DevicePowerSuppliesGroupName].Value = "PSUs";
                    //Set Logical Entity Display Name
                    newpsu.SCOM_Object_PowerSupply[mpp_EntityDisplayName].Value = "PSU-" + i.ToString();
                    // Set Fan Properties
                    newpsu.SCOM_Object_PowerSupply[mpp_PowerSupplyIndex].Value = Convert.ToInt32(SNMP.GetSNMP(SNMP.sysChassisPowerSupplyIndex + i.ToString(), Address, Port, Community)[0].Data.ToString());
                    newpsu.SCOM_Object_PowerSupply[mpp_PowerSupplyName].Value = "PSU-" + i.ToString();

                    PowerSupplies.Add(newpsu);
                }
            }
        }

        /// <summary>
        /// Get Temp Sensor
        /// </summary>
        private void GetTempSensors()
        {
            // Get Count of Temperature Sensors
            int TempSensorCount = Convert.ToInt16(SNMP.GetSNMP(SNMP.sysChassisTempNumber, Address, Port, Community)[0].Data.ToString());

            // If we have Temperature Sensors
            if (TempSensorCount > 0)
            {
                // Create Device Group Management Pack Class
                ManagementPackClass mpc_Device = SCOM_Functions.GetManagementPackClass("AP.F5.Device");

                // Create Root Entity Class & Display Name Prop
                ManagementPackClass mpc_Entity = SCOM_Functions.GetManagementPackClass("System.Entity");
                ManagementPackProperty mpp_EntityDisplayName = mpc_Entity.PropertyCollection["DisplayName"];

                // Parent Device Key Property (IP Address)
                ManagementPackProperty mpp_DeviceKey = mpc_Device.PropertyCollection["DeviceName"];

                // Create FanGroup Object
                ManagementPackClass mpc_DeviceTempSensorsGroup = SCOM_Functions.GetManagementPackClass("AP.F5.Device.TempSensorsGroup");
                SCOM_Object_TempSensorsGroup = new CreatableEnterpriseManagementObject(m_managementGroup, mpc_DeviceTempSensorsGroup);
                SCOM_Object_TempSensorsGroup[mpp_DeviceKey].Value = SystemNodeName; // Set Key of Device
                ManagementPackProperty mpp_DeviceTempSensorsGroupName = mpc_DeviceTempSensorsGroup.PropertyCollection["Name"];
                SCOM_Object_TempSensorsGroup[mpp_DeviceTempSensorsGroupName].Value = "Temp. Sensors";

                // Create Management Pack Class Objects for TempSensor and Needed Properties
                ManagementPackClass mpc_TempSensor = SCOM_Functions.GetManagementPackClass(className: "AP.F5.Device.TempSensor");
                ManagementPackProperty mpp_TempSensorIndex = mpc_TempSensor.PropertyCollection["Index"];
                ManagementPackProperty mpp_TempSensorName = mpc_TempSensor.PropertyCollection["Name"];

                // Set Fan-Group Relationship
                ManagementPackRelationship mpr_TempSensorsGroup = SCOM_Functions.GetManagementPackRelationship("AP.F5.Device.TempSensorsGroup.HostsTempSensors");

                // Create Temp Sensors
                for (int i = 1; i <= TempSensorCount; i++)
                {
                    TempSensor newTempSensor = new TempSensor();

                    // Create SCOM TempSensor Object
                    newTempSensor.SCOM_Object_TempSensor = new CreatableEnterpriseManagementObject(SCOM_Functions.m_managementGroup, mpc_TempSensor);
                    // Set Key of Device
                    newTempSensor.SCOM_Object_TempSensor[mpp_DeviceKey].Value = SystemNodeName;
                    // Set Key of Fans Group
                    newTempSensor.SCOM_Object_TempSensor[mpp_DeviceTempSensorsGroupName].Value = "Temp. Sensors";
                    //Set Logical Entity Display Name
                    newTempSensor.SCOM_Object_TempSensor[mpp_EntityDisplayName].Value = "TempSensor-" + i.ToString();
                    // Set Fan Properties
                    newTempSensor.SCOM_Object_TempSensor[mpp_TempSensorIndex].Value = Convert.ToInt32(SNMP.GetSNMP(SNMP.sysChassisTempIndex + i.ToString(), Address, Port, Community)[0].Data.ToString());
                    newTempSensor.SCOM_Object_TempSensor[mpp_TempSensorName].Value = "TempSensor-" + i.ToString();

                    TempSensors.Add(newTempSensor);
                }
            }

        }

        /// <summary>
        /// Get DiskPartitions
        /// </summary>
        private void GetDiskPartitions()
        {
            // Get
            var diskPartitions = SNMP.WalkSNMP(SNMP.sysHostDiskPartition, Address, Port, Community);

            if (diskPartitions.Count > 0)
            {
                // Create Device Management Pack Class
                ManagementPackClass mpc_Device = SCOM_Functions.GetManagementPackClass("AP.F5.Device");

                // Create Root Entity Class & Display Name Prop
                ManagementPackClass mpc_Entity = SCOM_Functions.GetManagementPackClass("System.Entity");
                ManagementPackProperty mpp_EntityDisplayName = mpc_Entity.PropertyCollection["DisplayName"];

                // Parent Device Key Property (IP Address)
                ManagementPackProperty mpp_DeviceKey = mpc_Device.PropertyCollection["DeviceName"];

                // Create DiskPartitions Group Object
                ManagementPackClass mpc_DiskPartitionGroup = SCOM_Functions.GetManagementPackClass("AP.F5.Device.DiskPartitionsGroup");
                SCOM_Object_DiskPartitionGroup = new CreatableEnterpriseManagementObject(m_managementGroup, mpc_DiskPartitionGroup);
                SCOM_Object_DiskPartitionGroup[mpp_DeviceKey].Value = SystemNodeName; // Set Key of Device
                ManagementPackProperty mpp_DiskPartitionGroupName = mpc_DiskPartitionGroup.PropertyCollection["Name"];
                SCOM_Object_DiskPartitionGroup[mpp_DiskPartitionGroupName].Value = "Disks";


                // Create Management Pack Class Objects for DiskPartition and Needed Properties
                ManagementPackClass mpc_DiskPartition = SCOM_Functions.GetManagementPackClass(className: "AP.F5.Device.DiskPartition");
                ManagementPackProperty mpp_DiskPartitionIndex = mpc_DiskPartition.PropertyCollection["Index"];
                ManagementPackProperty mpp_DiskPartitionMountpoint = mpc_DiskPartition.PropertyCollection["Mountpoint"];

                // Set DiskPartition-Group Relationship
                ManagementPackRelationship mpr_DiskPartitionsGroup = SCOM_Functions.GetManagementPackRelationship("AP.F5.Device.DiskPartitionsGroup.HostsDiskPartitions");


                for (int i = 0; i < diskPartitions.Count; i++)
                {
                    DiskPartition newDiskPartition = new DiskPartition();
                    string index = "." + diskPartitions[i].Id.ToString();
                    index = index.Replace(SNMP.sysHostDiskPartition, "");

                    // Create SCOM Fan Object
                    newDiskPartition.SCOM_Object_DiskPartition = new CreatableEnterpriseManagementObject(SCOM_Functions.m_managementGroup, mpc_DiskPartition);
                    // Set Key of Device
                    newDiskPartition.SCOM_Object_DiskPartition[mpp_DeviceKey].Value = SystemNodeName;
                    // Set Key of DiskPartitions Group
                    newDiskPartition.SCOM_Object_DiskPartition[mpp_DiskPartitionGroupName].Value = "Disks";
                    //Set Logical Entity Display Name
                    newDiskPartition.SCOM_Object_DiskPartition[mpp_EntityDisplayName].Value = diskPartitions[i].Data.ToString();
                    // Set Fan Properties
                    newDiskPartition.SCOM_Object_DiskPartition[mpp_DiskPartitionIndex].Value = index;
                    newDiskPartition.SCOM_Object_DiskPartition[mpp_DiskPartitionMountpoint].Value = diskPartitions[i].Data.ToString();

                    DiskPartitions.Add(newDiskPartition);
                }

            }

        }

    }
}
