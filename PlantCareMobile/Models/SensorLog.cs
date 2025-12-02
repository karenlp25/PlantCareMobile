using System;

namespace PlantCareMobile.Models
{
    public class SensorLog
    {
        // Coincide con id = db.Column(db.Integer...)
        public int Id { get; set; }

        // Coincide con device_id
        public int DeviceId { get; set; }

        // Coincide con temp
        public double Temp { get; set; }

        // Coincide con moisture_dirt
        public double MoistureDirt { get; set; }

        // Coincide con moisture_air
        public double MoistureAir { get; set; }

        // Coincide con raw_soil
        public double? RawSoil { get; set; }

        // Coincide con created_at
        public DateTime CreatedAt { get; set; }
    }
}