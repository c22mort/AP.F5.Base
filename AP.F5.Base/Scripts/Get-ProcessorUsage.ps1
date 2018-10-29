#==================================================================================
# Script: 	Get-ProcessorUsage.ps1
# Date:		11/10/18
# Author: 	Andi Patrick
# Purpose:	Gets Processor Usage via SNMP returns all as Property Bag
#==================================================================================

# Get the named parameters
param($Debug,$DeviceAddress,$DevicePort,$DeviceCommunity,$SharpSnmpLocation)

# Get Start Time For Script
$StartTime = (GET-DATE)

#Constants used for event logging
$SCRIPT_NAME			= 'Get-ProcessorUsage.ps1'
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
# bigipTrafficMgmt.bigipSystem.sysHostInfoStat.sysMultiHostCpu.sysMultiHostCpuTable.sysMultiHostCpuEntry.sysMultiHostCpuIndex
[string]$sysMultiHostCpuIndex = ".1.3.6.1.4.1.3375.2.1.7.5.2.1.2"
# bigipTrafficMgmt.bigipSystem.sysHostInfoStat.sysMultiHostCpu.sysMultiHostCpuTable.sysMultiHostCpuEntry.sysMultiHostCpuUsageRatio5m
[string]$sysMultiHostCpuUsageRatio5m = ".1.3.6.1.4.1.3375.2.1.7.5.2.1.35" 

Try {
	# Create endpoint for SNMP server
	$ip = [System.Net.IPAddress]::Parse($DeviceAddress)
	$svr = New-Object System.Net.IpEndPoint ($ip, $DevicePort)

	# Get ProcessorIndex from SNMP
	$ProcessorIndexList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
	[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunity, $sysMultiHostCpuIndex , $ProcessorIndexList, 3000, $walkMode)

	# Get ProcessorUsage from SNMP
	$ProcessorUsageList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
	[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunity, $sysMultiHostCpuUsageRatio5m , $ProcessorUsageList, 3000, $walkMode)


	# Loop Through Results
	For($i=0; $i -lt $ProcessorIndexList.Count; $i++) {

		# Get Processor Index from SNMP Result
		$snmpProcessorIndex = $sysMultiHostCpuIndex.TrimStart(".")
		$ProcessorIndex = $ProcessorIndexList[$i].id.ToString() 
		$ProcessorIndex = $ProcessorIndex -replace $snmpProcessorIndex, ""
		# Get ProcessorHealth
		$Percentage = $ProcessorUsageList[$i].Data.ToString()

		#Create a property bag.
		$bag = $api.CreatePropertyBag()
		$bag.AddValue("Index", $ProcessorIndex)
		$bag.AddValue("Percentage", [int]$Percentage)
		[string] $message = "Created Processor Property bag for $DeviceAddress`r`n" + "Index : " + $ProcessorIndex + "`r`n" + "Percentage : " + $Percentage
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