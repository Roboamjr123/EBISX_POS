﻿using EBISX_POS.API.Services.DTO.Order;
using Microsoft.Extensions.Options;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using static EBISX_POS.Services.AuthService;

namespace EBISX_POS.Services
{
    public class OrderService
    {
        private readonly ApiSettings _apiSettings;
        private readonly RestClient _restClient; // Use RestClient instead of HttpClient

        // Constructor to initialize RestClient and validate API configuration
        public OrderService(IOptions<ApiSettings> apiSettings)
        {
            _apiSettings = apiSettings.Value ?? throw new InvalidOperationException("API settings not provided.");

            // Check if the BaseUrl is configured properly
            if (string.IsNullOrEmpty(_apiSettings?.LocalAPI?.BaseUrl))
            {
                throw new InvalidOperationException("API BaseUrl is not configured.");
            }

            // Initialize RestClient with BaseUrl
            _restClient = new RestClient(_apiSettings.LocalAPI.BaseUrl);
        }

        // Validates that the OrderEndpoint is configured in the API settings
        private void ValidateOrderEndpoint()
        {
            if (string.IsNullOrEmpty(_apiSettings.LocalAPI.OrderEndpoint))
            {
                throw new InvalidOperationException("Order endpoint is not configured.");
            }
        }

        // Executes the REST request and returns a tuple indicating success and a response message
        private async Task<(bool, string)> ExecuteRequestAsync(RestRequest request)
        {
            // Execute the request asynchronously
            var response = await _restClient.ExecuteAsync(request);

            // Return success if the response is successful
            if (response.IsSuccessful)
            {
                return (true, response.Content ?? string.Empty);
            }

            // Handle specific error status codes
            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest => (false, response.Content ?? "Invalid request."),
                HttpStatusCode.Unauthorized => (false, "Unauthorized access. Please check your credentials."),
                _ => (false, $"Request failed with status code: {response.StatusCode}")
            };
        }

        // Calls the AddCurrentOrderVoid endpoint to void the current order
        public async Task<(bool, string)> AddCurrentOrderVoid(AddCurrentOrderVoidDTO voidOrder)
        {
            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL and create request with JSON body
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/AddCurrentOrderVoid";
                var request = new RestRequest(url, Method.Post).AddJsonBody(voidOrder);

                // Execute the request and return the result
                return await ExecuteRequestAsync(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }

        // Calls the AddOrderItem endpoint to add a new order item
        public async Task<(bool, string)> AddOrderItem(AddOrderDTO addOrder)
        {
            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL and create request with JSON body
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/AddOrderItem";
                var request = new RestRequest(url, Method.Post).AddJsonBody(addOrder);

                // Execute the request and return the result
                return await ExecuteRequestAsync(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }

        // Calls the EditQtyOrderItem endpoint to edit the quantity of an order item
        public async Task<(bool, string)> EditQtyOrderItem(EditOrderItemQuantityDTO editOrder)
        {
            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL and create request with JSON body using PUT method
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/EditQtyOrderItem";
                var request = new RestRequest(url, Method.Put).AddJsonBody(editOrder);

                // Execute the request and return the result
                return await ExecuteRequestAsync(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }

        // Calls the VoidOrderItem endpoint to void a specific order item
        public async Task<(bool, string)> VoidOrderItem(VoidOrderItemDTO voidOrder)
        {
            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL and create request with JSON body using PUT method
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/VoidOrderItem";
                var request = new RestRequest(url, Method.Put).AddJsonBody(voidOrder);

                // Execute the request and return the result
                var result = await ExecuteRequestAsync(request);
                return result.Item1
                    ? (true, result.Item2 ?? "Order voided successfully.")
                    : result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($" Error: {ex.Message}");
                return (false, "An unexpected error occurred.");
            }
        }

        // Calls the GetCurrentOrderItems endpoint to retrieve the current order items
        public async Task<List<GetCurrentOrderItemsDTO>> GetCurrentOrderItems()
        {
            try
            {
                ValidateOrderEndpoint(); // Validate API endpoint configuration

                // Build URL and create a GET request
                var url = $"{_apiSettings.LocalAPI.OrderEndpoint}/GetCurrentOrderItems";
                var request = new RestRequest(url, Method.Get);

                // Execute the request and return the data, or an empty list if null
                var response = await _restClient.ExecuteAsync<List<GetCurrentOrderItemsDTO>>(request);
                return response.Data ?? new List<GetCurrentOrderItemsDTO>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                return new List<GetCurrentOrderItemsDTO>();
            }
        }
    }
}
