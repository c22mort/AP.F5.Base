﻿#==================================================================================
# Script: 	Get-MemoryHealth.ps1
# Date:		11/10/18
# Author: 	Andi Patrick
# Purpose:	Gets Memory Health (total used %)via SNMP returns all as Property Bag
#==================================================================================

# Get the named parameters
param($Debug,$DeviceAddress,$DevicePort,$DeviceCommunity,$SharpSnmpLocation)

# Get Start Time For Script
$StartTime = (GET-DATE)

#Constants used for event logging
$SCRIPT_NAME			= 'Get-MemoryHealth.ps1'
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
# bigipTrafficMgmt.bigipSystem.sysHostInfoStat.sysHostMemory.sysHostMemoryTotalKb
[string]$sysHostMemoryTotalKb = ".1.3.6.1.4.1.3375.2.1.7.1.3"
# bigipTrafficMgmt.bigipSystem.sysHostInfoStat.sysHostMemory.sysHostMemoryUsedKb
[string]$sysHostMemoryUsedKb = ".1.3.6.1.4.1.3375.2.1.7.1.4"

Try 
{

	# Create endpoint for SNMP server
	$ip = [System.Net.IPAddress]::Parse($DeviceAddress)
	$svr = New-Object System.Net.IpEndPoint ($ip, $DevicePort)

	# Get Total Memory from SNMP
	$TotalMemoryList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
	[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunity, $sysHostMemoryTotalKb, $TotalMemoryList, 3000, $walkMode)

	# Get Used Memory from SNMP
	$UsedMemoryList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
	[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunity, $sysHostMemoryUsedKb, $UsedMemoryList, 3000, $walkMode)

	# Loop Through Results
	For($i=0; $i -lt $TotalMemoryList.Count; $i++) {
		
		# Get Memory Health
		$TotalMemory = $TotalMemoryList[$i].Data.ToString()
		$UsedMemory = $UsedMemoryList[$i].Data.ToString()
		[double]$Percentage = [math]::Round(($UsedMemory / $TotalMemory) * 100, 2)
		#Create a property bag.
		$bag = $api.CreatePropertyBag()
		$bag.AddValue("Percentage", $Percentage)
		[string] $message = "Created Memory Property bag for $DeviceAddress`r`n" + "Used Memory (%) : " + $Percentage
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