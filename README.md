# ComicCollector

## Project Overview

ComicCollector is a web application designed to help users manage their personal comic book and manga collections. It allows users to add, view, edit, and delete items from their collection. A key feature is the integration with external APIs like ComicVine (for comics) and MangaDex (for manga), enabling users to search for titles and add them directly to their collection with pre-filled details.

Furthermore, ComicCollector leverages the Google Gemini API to provide AI-powered features, including:
*   Personalized recommendations for new comics and manga based on the user's existing collection.
*   Enrichment of comic/manga data with details like author, publisher, and plot summaries.
*   Generation of review summaries (pros and cons) for items in the collection.

## Problem it Solves

For comic and manga enthusiasts, keeping track of a growing collection can be challenging. ComicCollector aims to solve this by:

*   Providing a centralized digital platform to catalog their items.
*   Simplifying the data entry process by fetching information from established databases (ComicVine, MangaDex).
*   Helping users discover new titles through AI-driven recommendations tailored to their tastes.
*   Enhancing existing collection data with more comprehensive information through AI.
*   Offering AI-generated insights (pros/cons) for items in their collection.

## Key Features

*   **User Authentication:** Secure registration and login system using ASP.NET Core Identity.
*   **Collection Management:**
    *   Add comics/manga manually or by searching external APIs.
    *   View collection with pagination and statistics (total series, authors, publishers).
    *   Edit existing item details.
    *   Delete items from the collection.
*   **Discover Page:**
    *   Search ComicVine and MangaDex for new titles.
    *   View featured/trending content.
    *   Add search results directly to the personal collection.
*   **AI-Powered Enhancements (Google Gemini):**
    *   **Recommendations:** Get personalized comic and manga suggestions based on the current collection.
    *   **Data Enrichment:** Automatically fetch and update missing details (author, publisher, description, publication date) for items in the collection.
    *   **Review Summaries:** View AI-generated pros and cons for specific comics/manga in the collection details modal.
*   **Detailed Item View:** Modal pop-up showing comprehensive details of a collected item, including AI-generated review summaries.
*   **Responsive Design:** User interface styled with Bootstrap for usability across different devices.

## Technologies Used

*   **Backend:**
    *   ASP.NET Core 6.0 (using Razor Pages)
    *   C#
    *   Entity Framework Core (with SQL Server) for database operations
    *   ASP.NET Core Identity for user authentication and authorization
*   **Frontend:**
    *   HTML5, CSS3
    *   Bootstrap 5 for styling and responsive design
    *   JavaScript (jQuery) for dynamic content, AJAX calls, and UI interactions (e.g., loading spinners).
*   **APIs & Services:**
    *   **ComicVine API:** For fetching comic book data.
    *   **MangaDex API:** For fetching manga data.
    *   **Google Gemini API:** For AI-powered recommendations, data enrichment, and review summaries.
*   **Logging:**
    *   Microsoft.Extensions.Logging (ILogger) for application logging.
*   **Other:**
    *   HttpClient for making API requests.
    *   Session Management for user session data.
    *   User Secrets for API key management during development.

## Conclusion

ComicCollector is a robust and feature-rich application for comic and manga enthusiasts. By integrating powerful external APIs and leveraging cutting-edge AI capabilities through the Gemini API, it offers a streamlined, intelligent, and engaging way to organize, expand, and learn more about one's collection. It serves as a practical example of building a modern web application with ASP.NET Core, incorporating external services, and utilizing AI to enhance user experience.

---
*This README was generated with assistance from an AI coding agent.*