from flask import Flask, request, jsonify
from flask_cors import CORS
import openai

app = Flask(__name__)
CORS(app,  resources={r"/api/*": {"origins": "http://localhost:3000"}})

# Initialize OpenAI with your API key
openai.api_key = "94acf53489374ddaaaeaf59e45edb351"

@app.route('/api/gpt3', methods=['POST'])
def run_gpt3():
    data = request.json
    prompt = data.get('prompt', '')
    response = openai.Completion.create(engine="text-davinci-002", prompt=prompt, max_tokens=150)
    return jsonify(response)

if __name__ == '__main__':
    app.run(debug=True)