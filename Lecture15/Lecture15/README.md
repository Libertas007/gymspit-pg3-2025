# Jinja2/Flask Template Compiler

A production-quality C# static site generator that compiles Flask/Jinja2 HTML templates into standalone static HTML files. This tool enables frontend developers to preview Flask templates without running Python or Flask.

## Features

- **Full Jinja2 Syntax Support**
  - Template inheritance (`{% extends %}`, `{% block %}`)
  - Includes (`{% include %}`)
  - Control flow (`{% if %}`, `{% for %}`, `{% set %}`)
  - Variables with filters (`{{ variable|filter }}`)
  - Macros (`{% macro %}`)
  - Comments (`{# comment #}`)
  - Raw blocks (`{% raw %}`)

- **CSS Handling**
  - **Copy mode**: Copies CSS files to output directory, updates paths
  - **Inline mode**: Inlines CSS directly into `<style>` tags
  - **Passthrough mode**: Leaves CSS references unchanged

- **Flask Integration**
  - Resolves `url_for('static', filename=...)` calls
  - Configurable static asset root directory
  - Support for nested static directories

- **Developer Experience**
  - Watch mode with auto-rebuild on file changes
  - Verbose logging and warnings
  - Template validation command
  - Content hashing for cache busting

## Installation

Build from source:

```bash
cd Lecture15
dotnet build
```

## Usage

### Basic Build

Compile all templates to static HTML:

```bash
dotnet run -- build -t ./templates -o ./dist -d ./data/mock_data.json
```

### With Static Assets (Copy Mode)

Copy CSS files to output and update references:

```bash
dotnet run -- build \
  --templates ./Examples/templates \
  --output ./dist \
  --data ./Examples/data/mock_data.json \
  --static-root ./Examples/static \
  --css-mode copy \
  --verbose
```

### With Inline CSS

Inline all CSS directly into the HTML:

```bash
dotnet run -- build \
  -t ./Examples/templates \
  -o ./dist \
  -d ./Examples/data/mock_data.json \
  -s ./Examples/static \
  --css-mode inline \
  --minify-css
```

### Watch Mode

Automatically rebuild when files change:

```bash
dotnet run -- watch \
  -t ./Examples/templates \
  -o ./dist \
  -d ./Examples/data/mock_data.json \
  -s ./Examples/static \
  --css-mode copy
```

### Validate Templates

Check templates for errors without generating output:

```bash
dotnet run -- validate \
  -t ./Examples/templates \
  -d ./Examples/data/mock_data.json \
  -s ./Examples/static
```

### List Templates

Show all available templates:

```bash
dotnet run -- list -t ./Examples/templates
```

## Command Reference

### Global Options

| Option | Alias | Description |
|--------|-------|-------------|
| `--templates` | `-t` | Template directory (required) |
| `--output` | `-o` | Output directory (required for build) |
| `--data` | `-d` | JSON file with mock data |
| `--verbose` | `-v` | Enable detailed logging |

### Build Command Options

| Option | Description | Default |
|--------|-------------|---------|
| `--static-root`, `-s` | Static assets directory | None |
| `--css-mode` | CSS handling: `copy`, `inline`, `passthrough` | `copy` |
| `--static-prefix` | URL prefix for static assets | `/static/` |
| `--hash-assets` | Add content hash to filenames | `false` |
| `--minify-css` | Minify CSS in inline mode | `false` |
| `--no-escape` | Disable HTML auto-escaping | `false` |
| `--strict` | Error on missing variables | `false` |
| `--template`, `-T` | Compile specific template only | All |

## Directory Structure

Recommended project structure:

```
project/
??? templates/
?   ??? layouts/
?   ?   ??? base.html         # Base template with {% block %} definitions
?   ??? partials/
?   ?   ??? _sidebar.html     # Partial templates (prefixed with _)
?   ??? macros/
?   ?   ??? components.html   # Reusable macros
?   ??? index.html            # Page templates
?   ??? products.html
?   ??? about.html
??? static/
?   ??? css/
?   ?   ??? main.css
?   ?   ??? components.css
?   ??? js/
?   ?   ??? app.js
?   ??? images/
?       ??? logo.png
??? data/
?   ??? mock_data.json        # Template variables
??? dist/                     # Generated output
    ??? index.html
    ??? products.html
    ??? about.html
    ??? static/
        ??? css/
            ??? main.css
            ??? components.css
```

## Mock Data Format

The mock data file is a JSON object with template variables:

```json
{
  "site": {
    "name": "My Flask App",
    "version": "1.0.0"
  },
  "user": {
    "name": "John Doe",
    "is_authenticated": true
  },
  "navigation": [
    { "url": "/", "title": "Home", "active": true },
    { "url": "/about", "title": "About", "active": false }
  ],
  "products": [
    { "id": 1, "name": "Product One", "price": 29.99 }
  ]
}
```

## Template Examples

### Base Template with External CSS

```html
<!DOCTYPE html>
<html>
<head>
    <title>{% block title %}{{ site.name }}{% endblock %}</title>
    <link rel="stylesheet" href="{{ url_for('static', filename='css/main.css') }}">
    {% block extra_styles %}{% endblock %}
</head>
<body>
    {% block content %}{% endblock %}
</body>
</html>
```

### Child Template

```html
{% extends "layouts/base.html" %}

{% block title %}Products - {{ site.name }}{% endblock %}

{% block content %}
<h1>Our Products</h1>
{% for product in products %}
<div class="product">
    <h2>{{ product.name }}</h2>
    <p class="price">${{ "%.2f"|format(product.price) }}</p>
</div>
{% endfor %}
{% endblock %}
```

### Using Macros

```html
{% from "macros/components.html" import product_card %}

{% for product in products %}
    {{ product_card(product, show_details=true) }}
{% endfor %}
```

## CSS Processing Details

### Copy Mode (Default)

- CSS files are copied to `{output}/static/css/`
- `<link>` href attributes are updated to new paths
- Relative URLs within CSS (fonts, images) are also copied
- Duplicate CSS files are only copied once

### Inline Mode

- CSS content is injected into a `<style>` tag in `<head>`
- `@import` statements are resolved and inlined
- Original `<link>` tags are removed
- Optional minification removes whitespace and comments

### Passthrough Mode

- CSS references remain unchanged
- Useful when CSS is hosted elsewhere

## Supported Jinja2 Features

### Tags

- `{% extends "template" %}` - Template inheritance
- `{% block name %}...{% endblock %}` - Overridable blocks
- `{% include "template" %}` - Include another template
- `{% if condition %}...{% elif %}...{% else %}...{% endif %}`
- `{% for item in items %}...{% else %}...{% endfor %}`
- `{% set variable = value %}` - Set variables
- `{% macro name(args) %}...{% endmacro %}` - Define macros
- `{% import "macros" as m %}` - Import macros
- `{% from "macros" import name %}` - Selective import
- `{% with var = value %}...{% endwith %}` - Scoped variables
- `{% raw %}...{% endraw %}` - Unprocessed content
- `{# comment #}` - Comments

### Filters

String: `upper`, `lower`, `capitalize`, `title`, `trim`, `truncate`, `replace`, `striptags`, `escape`, `safe`

Numbers: `abs`, `round`, `int`, `float`

Collections: `length`, `first`, `last`, `reverse`, `sort`, `join`, `unique`, `sum`, `min`, `max`, `map`, `selectattr`, `groupby`

Formatting: `default`, `tojson`, `format`, `filesizeformat`

### Tests

`defined`, `undefined`, `none`, `true`, `false`, `string`, `number`, `sequence`, `mapping`, `odd`, `even`, `empty`, `divisibleby(n)`

### Functions

- `range(start, stop, step)` - Generate number sequences
- `url_for('static', filename='path')` - Static file URLs

## Error Handling

The compiler handles errors gracefully:

- **Missing templates**: Error with clear path information
- **Circular inheritance**: Detected and reported
- **Missing variables**: Configurable behavior (empty, error, or placeholder)
- **Missing CSS files**: Warning with path information
- **Parse errors**: Line and column information

## Limitations

- No Python expression evaluation (use mock data instead)
- No custom Python filters (built-in filters only)
- No Flask context processors
- External URLs in CSS are not processed

## Architecture

The compiler follows clean architecture principles:

```
Core/
??? Ast/           - Abstract Syntax Tree nodes
??? Parsing/       - Lexer and Parser
??? Resolution/    - Inheritance and block resolution
??? Rendering/     - Template rendering engine
??? Context/       - Variable management
??? Filters/       - Built-in filter implementations
??? Assets/        - CSS and static asset processing
??? Loading/       - Template file loading

Cli/
??? JinjaCompilerCli.cs - Command-line interface
```

## License

MIT License
