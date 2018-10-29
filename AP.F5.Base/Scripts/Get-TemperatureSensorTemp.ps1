#==================================================================================
# Script: 	Get-TEmperatureSensorTemp.ps1
# Date:		12/10/18
# Author: 	Andi Patrick
# Purpose:	Gets Temperature Sensor Temp via SNMP returns all as Property Bag
#==================================================================================

# Get the named parameters
param($Debug,$DeviceAddress,$DevicePort,$DeviceCommunity,$SharpSnmpLocation)

# Get Start Time For Script
$StartTime = (GET-DATE)

#Constants used for event logging
$SCRIPT_NAME			= 'Get-TemperatureSensorTemp.ps1'
$EVENT_LEVEL_ERROR 		= 1
$VENT_LEVEL_WARNING 	= 2
$EVENT_LEVEL_INFO 		= 4

$SCRIPT_STARTED				= 4601
$SCRIPT_PROPERTYBAG_CREATED	= 4602
$SCRIPT_EVENT				= 4603
$SCRIPT_ENDED				= 4604
$SCRIPT_ERROR				= 4605

#==================================================================================
# Sub:		LogDebugEvent
# Purpose:	Logs an informational event to the Operations Manager event log
#			only if Debug argument is true
#==================================================================================
function Log-DebugEvent
{
	param($eventNo,$message)

	$message = "`n" + $message
	if ($Debug -eq $true)
	{
		$api.LogScriptEvent($SCRIPT_NAME,$eventNo,$EVENT_LEVEL_INFO,$message)
	}
}

#Start by setting up API object.
$api = New-Object -comObject 'MOM.ScriptAPI'

# Log Startup Message
Log-DebugEvent $SCRIPT_STARTED "Script Started."

# Load SharpSNMPLib
[reflection.assembly]::LoadFrom( (Resolve-Path $SharpSnmpLocation) )

# Use SNMP v2
$ver = [Lextm.SharpSnmpLib.VersionCode]::V2

# Set Walk Mode to WithinSubtree
$walkMode = [Lextm.SharpSnmpLib.Messaging.WalkMode]::WithinSubtree

# OIDs used
# bigipTrafficMgmt.bigipSystem.sysPlatform.sysChassis.sysChassisTemp.sysChassisTempTable.sysChassisTempEntry.sysChassisTempIndex
[string]$sysChassisTempIndex = ".1.3.6.1.4.1.3375.2.1.3.2.3.2.1.1"
# bigipTrafficMgmt.bigipSystem.sysPlatform.sysChassis.sysChassisTemp.sysChassisTempTable.sysChassisTempEntry.sysChassisTempTemperature
[string]$sysChassisTempTemperature = ".1.3.6.1.4.1.3375.2.1.3.2.3.2.1.2" 

Try {
	# Create endpoint for SNMP server
	$ip = [System.Net.IPAddress]::Parse($DeviceAddress)
	$svr = New-Object System.Net.IpEndPoint ($ip, $DevicePort)

	# Get TemperatureSensorIndex from SNMP
	$IndexList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
	[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunity, $sysChassisTempIndex , $IndexList, 3000, $walkMode)

	# Get TemperatureSensorHealth from SNMP
	$TemperatureList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
	[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunity, $sysChassisTempTemperature , $TemperatureList, 3000, $walkMode)


	# Loop Through Results
	For($i=0; $i -lt $IndexList.Count; $i++) {
		
		# Get TEmperature Sensor Index from SNMP Result
		$snmpTempSensorIndex = $sysChassisTempIndex.TrimStart(".")
		$TempSensorIndex = $IndexList[$i].id.ToString() 
		$TempSensorIndex = $TempSensorIndex -replace $snmpTempSensorIndex, ""
		$TempSensorIndex = $TempSensorIndex.TrimStart(".")

		# Get Temperature
		$Temperature = $TemperatureList[$i].Data.ToString()

		#Create a property bag.
		$bag = $api.CreatePropertyBag()
		$bag.AddValue("Index", [int]$TempSensorIndex)
		$bag.AddValue("Temperature", [int]$Temperature)
		[string] $message = "Created Temperature Sensor Property bag for $DeviceAddress`r`n" + "Index : " + $TempSensorIndex + "`r`n" + "Temp. : " + $Temperature
		Log-DebugEvent $SCRIPT_PROPERTYBAG_CREATED $message
		#$api.Return($bag)
		$bag		
	}
}
Catch
{
	$message = "SNMP Server : $DeviceAddress" + "`r`nSNMP Port : " + $DevicePort + "`r`nSNMP Community : " + $DeviceCommunity + "`r`nSharpSnmp Location : " + $SharpSnmpLocation + "`r`nError : $_"
	Log-DebugEvent $SCRIPT_ERROR $message
}

# Get End Time For Script
$EndTime = (GET-DATE)
$TimeTaken = NEW-TIMESPAN -Start $StartTime -End $EndTime
$Seconds = [math]::Round($TimeTaken.TotalSeconds, 2)
# Log Finished Message
Log-DebugEvent $SCRIPT_ENDED "Script Finished. Took $Seconds Seconds to Complete!"