import React, { useState } from 'react';
import axios from 'axios';

const App = () => {
  const [inputText, setInputText] = useState('');
  const [responseText, setResponseText] = useState('');

  const handleInputChange = (e) => {
    setInputText(e.target.value);
  };

  const handleSubmit = async () => {
    console.log("wubmitting");
    try {
      const response = await axios.post('http://localhost:5000/api/gpt3', { prompt: inputText });
      setResponseText(response.data);
    } catch (error) {
      console.error('Error:', error);
      setResponseText('An error occurred while processing your request.');
    }
  };

  return (
    <div>
      <h1>React Python OpenAI Service</h1>
      <textarea
        rows={4}
        cols={50}
        value={inputText}
        onChange={handleInputChange}
        placeholder="Enter your prompt here..."
      />
      <br />
      <button onClick={handleSubmit}>Submit</button>
      <div>
        <h2>Response:</h2>
        <p>{responseText}</p>
      </div>
    </div>
  );
};

export default App;
