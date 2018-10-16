﻿#==================================================================================
# Script: 	Get-DeviceClientSideOutboundKb.ps1
# Date:		11/10/18
# Author: 	Andi Patrick
# Purpose:	Gets Device ClientSide Inbound Kb via SNMP returns as Property Bag
#==================================================================================

# Get the named parameters
param($Debug,$DeviceAddress,$DevicePort,$DeviceCommunity,$SharpSnmpLocation)

#Constants used for event logging
$SCRIPT_NAME			= 'Get-DeviceClientSideOutboundKb.ps1'
$EVENT_LEVEL_ERROR 		= 1
$VENT_LEVEL_WARNING 	= 2
$EVENT_LEVEL_INFO 		= 4

$SCRIPT_STARTED			= 4601
$PROPERTYBAG_CREATED	= 4602
$SCRIPT_EVENT			= 4603
$SCRIPT_ENDED			= 4604

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
$message =	$SCRIPT_NAME + " Started."
Log-DebugEvent $SCRIPT_STARTED " Script Started."

# Load SharpSNMPLib
[reflection.assembly]::LoadFrom( (Resolve-Path $SharpSnmpLocation) )

# Use SNMP v2
$ver = [Lextm.SharpSnmpLib.VersionCode]::V2

# Set Walk Mode to WithinSubtree
$walkMode = [Lextm.SharpSnmpLib.Messaging.WalkMode]::WithinSubtree

# OIDs used
# bigipTrafficMgmt.bigipSystem.sysGlobals.sysGlobalStats.sysGlobalStat.sysStatClientBytesOut5m
[string]$sysStatClientBytesOut5m = ".1.3.6.1.4.1.3375.2.1.1.2.1.90.0"

Try 
{

	# Create endpoint for SNMP server
	$ip = [System.Net.IPAddress]::Parse($DeviceAddress)
	$svr = New-Object System.Net.IpEndPoint ($ip, $DevicePort)

	# Create List for SNMP Get
	$vList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
	$oidClient = New-Object Lextm.SharpSnmpLib.ObjectIdentifier ($sysStatClientBytesOut5m)
	$vList.Add($oidClient)

	Try {
		# Get SNMP Results
		$results = [Lextm.SharpSnmpLib.Messaging.Messenger]::Get($ver, $svr, $DeviceCommunity, $vList, 3000)
		# As Long as we have one result we can continue
		if ($Results.Count -gt 0) {
			[int]$NetworkBytes = $results[0].Data.ToString()
			[int]$NetworkKb = $NetworkBytes / 1024
			# Create a property bag.
			$bag = $api.CreatePropertyBag()
			$bag.AddValue("NetworkKb", [int]$NetworkKb)
			[string] $message = " Created Property bag for $DeviceAddress`r`n`r`n" 	+ "Client-Side OutBound (Kb): "  + $NetworkKb + "`r`n"
			Log-DebugEvent $SCRIPT_EVENT $message
			#$api.Return($bag)
			$bag		
		}
	} Catch {
		Log-DebugEvent $SCRIPT_EVENT "SNMP Get Error : $_"
	}

}
Catch
{
	Log-DebugEvent $SCRIPT_EVENT "Could not Contact $DeviceAddress"
}

# Log Finished Message
Log-DebugEvent $SCRIPT_ENDED " Script Ended."