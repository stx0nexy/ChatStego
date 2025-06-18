import { Box, TextField, Button } from '@mui/material';

export const AddContactModal = ({ newContactId, setNewContactId, onAddContact }) => (
  <Box 
  sx={{ 
    bottom: 0, 
    width: '100%',
    display: 'flex', 
    backgroundColor: 'white', 
    padding: 1,
    zIndex: 1000,
    alignItems: 'center',
  }}
  >
    <TextField
      variant="outlined"
      placeholder="Enter contact email"
      value={newContactId}
      onChange={(e) => setNewContactId(e.target.value)}
    />
    <Button variant="contained" color="primary" onClick={onAddContact} sx={{ ml: 1 }}>
      Add Contact
    </Button>
  </Box>
);