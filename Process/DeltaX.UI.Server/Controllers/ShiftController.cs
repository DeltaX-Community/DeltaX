using DeltaX.Modules.Shift;
using DeltaX.Modules.Shift.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;


[Route("api/v1/[controller]")]
[ApiController]
public class ShiftController : ControllerBase
{
    private readonly IShiftService service;

    public ShiftController(IShiftService service)
    {
        this.service = service;
    }

    [HttpGet("ShiftCrew/{*profileName}")]
    public ShiftCrewDto GetShiftCrew(
        string profileName,
        [FromQuery] DateTime? date = null)
    {
        return service.GetShiftCrew(profileName, date);
    }
     
    [HttpGet("ShiftProfile/{*profileName}")]
    public ShiftProfileDto GetShiftProfile(
        string profileName,
        [FromQuery] DateTime? start = null,
        [FromQuery] DateTime? end = null)
    {
        return service.GetShiftProfile(profileName, start, end);
    } 
}