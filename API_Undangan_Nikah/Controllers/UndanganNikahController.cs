﻿using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using System.Text.RegularExpressions;

namespace API_Undangan_Nikah.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UndanganNikahController : Controller
    {
        private readonly IConfiguration configuration;

        public UndanganNikahController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> AddUcapan([FromBody] Dictionary<string, object> data)
        {
            if (data == null || !data.ContainsKey("ucapan") || !data.ContainsKey("konfirmasi_kehadiran"))
            {
                return BadRequest("Ucapan dan Konfirmasi Kehadiran wajib ada!");
            }

            try
            {
                string ucapan = data["ucapan"].ToString();
                string konfirmasiKehadiran = data["konfirmasi_kehadiran"].ToString();

                // Validasi
                if (string.IsNullOrWhiteSpace(ucapan))
                {
                    return BadRequest("Ucapan tidak boleh kosong!");
                }
                if (string.IsNullOrWhiteSpace(konfirmasiKehadiran))
                {
                    return BadRequest("Konfirmasi Kehadiran tidak boleh kosong!");
                }

                string pattern = @"^[a-zA-Z.,\s]+$";
                Regex regex = new Regex(pattern);
                if (!regex.IsMatch(ucapan))
                {
                    return BadRequest("Ucapan hanya boleh berisi huruf, titik, koma, dan spasi.");
                }
                if (!regex.IsMatch(konfirmasiKehadiran))
                {
                    return BadRequest("Konfirmasi Kehadiran hanya boleh berisi huruf, titik, koma, dan spasi.");
                }

                var query = @"
                    INSERT INTO ucapan (Ucapan, Konfirmasi_Kehadiran, Addtime) VALUES (@ucapan, @konfirmasi_kehadiran, NOW());
                ";

                using (var con = new MySqlConnection(configuration.GetConnectionString("DefaultConnection")))
                {
                    await con.OpenAsync();

                    using (var cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ucapan", ucapan);
                        cmd.Parameters.AddWithValue("@konfirmasi_kehadiran", konfirmasiKehadiran);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok("Ucapan dan konfirmasi kehadiran berhasil ditambahkan.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUcapan()
        {
            try
            {
                var ucapanList = new List<Dictionary<string, object>>();

                var query = @"
                    SELECT * FROM ucapan;
                ";

                using (var con = new MySqlConnection(configuration.GetConnectionString("DefaultConnection")))
                {
                    await con.OpenAsync();

                    using (var cmd = new MySqlCommand(query, con))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var ucapanDb = reader.IsDBNull(reader.GetOrdinal("Ucapan")) ? string.Empty : reader.GetString("Ucapan");
                                var konfirmasiKehadiranDb = reader.IsDBNull(reader.GetOrdinal("Konfirmasi_Kehadiran")) ? string.Empty : reader.GetString("Konfirmasi_Kehadiran");
                                var addtimeDb = reader.IsDBNull(reader.GetOrdinal("addtime")) ? DateTime.MinValue : reader.GetDateTime("addtime");

                                var ucapan = new Dictionary<string, object>
                                {
                                    { "Ucapan", ucapanDb },
                                    { "Konfirmasi_Kehadiran", konfirmasiKehadiranDb },
                                    { "Addtime", addtimeDb }
                                };
                                ucapanList.Add(ucapan);
                            }
                        }
                    }
                }

                return Ok(ucapanList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}