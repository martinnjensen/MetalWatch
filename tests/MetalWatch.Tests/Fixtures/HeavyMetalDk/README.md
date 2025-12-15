# HTML Test Fixtures for HeavyMetalDk Scraper

## Purpose

This directory contains HTML fixtures for testing the HeavyMetalDkScraper without requiring live network requests.

## Fixture Files

- **full-calendar-2025-12-15.html**: Representative sample of a concert calendar page
- **single-concert.html**: Minimal single concert entry
- **festival-event.html**: Multi-artist festival with <strong> tags
- **cancelled-concert.html**: Concert with "Aflyst" marker
- **new-concert.html**: Concert with "Ny" marker
- **year-rollover.html**: December to January transition
- **empty-page.html**: Page with no concerts

## Refreshing Fixtures

To refresh these fixtures with real data:

1. Visit https://heavymetal.dk/koncertkalender?landsdel=koebenhavn
2. Save the page source (View Source → Save As)
3. Replace the appropriate fixture file
4. Verify tests still pass
5. Update this README with the refresh date

**Last Refreshed**: 2025-12-15

## License Note

These fixtures are created for educational and testing purposes only.
The structure and format are based on public web pages from heavymetal.dk.
Respect the original site's copyright and terms of service.
